using System.Text;
using System.Text.Json;
using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Sofia;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Application.Sofia;

public sealed class SofiaEcdAnalysisService(
    IValidationSubmissionService submission,
    IValidationExecutionRepository executions,
    ILogger<SofiaEcdAnalysisService> logger)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxWaitPerDocument = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MaxWaitOverallCap = TimeSpan.FromMinutes(12);

    public async Task<ResultadoAnaliseSofia> AnalyzeAsync(
        SolicitacaoAnaliseSofia solicitacao,
        CancellationToken cancellationToken = default)
    {
        var documentos = solicitacao.Documentos;
        if (documentos.Count == 0)
        {
            return ErrorResult(solicitacao, "SEM_DOCUMENTOS", "Nenhum documento na solicitação.");
        }

        var userId = $"sofia:{solicitacao.IdExecucao:N}";
        if (userId.Length > 64)
        {
            userId = userId[..64];
        }

        var overallDeadline = DateTimeOffset.UtcNow.Add(
            TimeSpan.FromMinutes(Math.Min(MaxWaitOverallCap.TotalMinutes, MaxWaitPerDocument.TotalMinutes * documentos.Count)));

        var allAchados = new List<SofiaAchado>();
        var executionIds = new List<Guid>();
        ValidationExecution? lastCompleted = null;
        var failures = new List<(string Codigo, string Mensagem)>();

        foreach (var doc in documentos)
        {
            if (DateTimeOffset.UtcNow >= overallDeadline)
            {
                failures.Add(("TIMEOUT_VALIDACAO", "Tempo total esgotado antes de analisar todos os arquivos."));
                break;
            }

            var docResult = await AnalyzeSingleDocumentAsync(
                solicitacao,
                doc,
                userId,
                overallDeadline,
                cancellationToken);

            if (docResult.Error is { } err)
            {
                failures.Add(err);
                logger.LogWarning(
                    "Falha ao validar documento {Documento} para SOFIA {IdSolicitacao}: {Codigo}",
                    doc.IdDocumento,
                    solicitacao.IdSolicitacao,
                    err.Codigo);
                continue;
            }

            executionIds.Add(docResult.ExecutionId);
            lastCompleted = docResult.Execution;
            allAchados.AddRange(docResult.Achados);
        }

        if (allAchados.Count == 0 && failures.Count == documentos.Count)
        {
            var first = failures[0];
            return ErrorResult(solicitacao, first.Codigo, first.Mensagem);
        }

        var analise = BuildAnaliseSummary(documentos.Count, allAchados.Count, failures.Count);
        var observacoes = BuildObservacoes(documentos.Count, lastCompleted, failures.Count);

        return new ResultadoAnaliseSofia
        {
            VersaoContrato = SofiaContractValidator.ContractVersion,
            IdExecucao = solicitacao.IdExecucao,
            IdSolicitacao = solicitacao.IdSolicitacao,
            Status = allAchados.Count == 0 ? "concluido" : "parcial",
            Analise = analise,
            Achados = allAchados,
            Observacoes = observacoes,
            IdValidacaoInterna = executionIds.FirstOrDefault(),
            IdValidacoesInternas = executionIds,
        };
    }

    private static string BuildAnaliseSummary(int documentCount, int achadoCount, int failureCount)
    {
        if (achadoCount == 0 && failureCount == 0)
        {
            return documentCount == 1
                ? "Nenhuma inconsistência detectada nas regras ativas do catálogo."
                : $"Nenhuma inconsistência detectada em {documentCount} arquivos ECD analisados.";
        }

        if (achadoCount == 0 && failureCount > 0)
        {
            return $"Não foi possível concluir a análise de {failureCount} arquivo(s); os demais não apresentaram inconsistências.";
        }

        return failureCount > 0
            ? $"Foram identificadas {achadoCount} inconsistência(s) em {documentCount} arquivo(s) ({failureCount} arquivo(s) com falha de processamento)."
            : documentCount == 1
                ? $"Foram identificadas {achadoCount} inconsistência(s) na validação ECD."
                : $"Foram identificadas {achadoCount} inconsistência(s) em {documentCount} arquivos ECD.";
    }

    private static List<string> BuildObservacoes(int documentCount, ValidationExecution? lastExecution, int failureCount)
    {
        var observacoes = new List<string>
        {
            documentCount == 1
                ? "1 arquivo ECD analisado."
                : $"{documentCount} arquivos ECD analisados.",
        };

        if (failureCount > 0)
        {
            observacoes.Add($"{failureCount} arquivo(s) não puderam ser processados (timeout ou falha interna).");
        }

        if (lastExecution is not null)
        {
            observacoes.AddRange(SofiaObservacoesBuilder.Build(lastExecution, lastExecution.ReportJson));
        }

        return observacoes;
    }

    private async Task<DocumentAnalysisResult> AnalyzeSingleDocumentAsync(
        SolicitacaoAnaliseSofia solicitacao,
        SofiaDocumento doc,
        string userId,
        DateTimeOffset overallDeadline,
        CancellationToken cancellationToken)
    {
        var fileName = doc.Nome!.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
            ? doc.Nome
            : $"{doc.Nome}.txt";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(doc.Texto!));
        var correlationId = $"{solicitacao.IdSolicitacao:N}:{doc.IdDocumento}";

        ValidationSubmissionResult submitted;
        try
        {
            submitted = await submission.SubmitAsync(
                stream,
                fileName,
                "text/plain",
                userId,
                correlationId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao submeter ECD {Documento}", doc.IdDocumento);
            return DocumentAnalysisResult.Failed("SUBMISSAO_ECD", ex.Message);
        }

        var execution = await WaitForCompletionAsync(submitted.ExecutionId, overallDeadline, cancellationToken);
        if (execution is null)
        {
            return DocumentAnalysisResult.Failed(
                "TIMEOUT_VALIDACAO",
                $"Validação não concluiu a tempo (documento {doc.Nome}).");
        }

        if (execution.Status == ValidationStatus.Failed)
        {
            return DocumentAnalysisResult.Failed(
                execution.ErrorCode ?? "VALIDACAO_FALHOU",
                execution.ErrorMessage ?? "Pipeline ECD retornou falha.");
        }

        if (execution.Status == ValidationStatus.Cancelled)
        {
            return DocumentAnalysisResult.Failed("VALIDACAO_CANCELADA", "Validação ECD cancelada.");
        }

        if (execution.Status != ValidationStatus.Completed)
        {
            return DocumentAnalysisResult.Failed(
                "TIMEOUT_VALIDACAO",
                $"Validação não concluiu (documento {doc.Nome}, status={execution.Status}).");
        }

        var achados = execution.Findings
            .Select(f => MapFinding(f, doc.IdDocumento!))
            .ToList();

        return DocumentAnalysisResult.Succeeded(submitted.ExecutionId, execution, achados);
    }

    private async Task<ValidationExecution?> WaitForCompletionAsync(
        Guid executionId,
        DateTimeOffset overallDeadline,
        CancellationToken cancellationToken)
    {
        while (DateTimeOffset.UtcNow < overallDeadline)
        {
            var execution = await executions.GetWithFindingsAsync(executionId, cancellationToken);
            if (execution?.Status is ValidationStatus.Completed or ValidationStatus.Failed or ValidationStatus.Cancelled)
            {
                return execution;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        return null;
    }

    private static SofiaAchado MapFinding(Finding finding, string documentoId) =>
        new()
        {
            IdAchado = finding.Id.ToString("N"),
            Descricao = finding.Summary,
            IdDocumentos = [documentoId],
            Evidencias = RuleEvidenceFormatter.ParseExplanationLines(finding.Explanation).ToList(),
            Severidade = MapSeverity(finding.Severity),
        };

    private static string MapSeverity(FindingSeverity severity) => severity switch
    {
        FindingSeverity.Critical => "critica",
        FindingSeverity.High => "alta",
        FindingSeverity.Medium => "media",
        FindingSeverity.Low => "baixa",
        _ => "baixa",
    };

    private static ResultadoAnaliseSofia ErrorResult(
        SolicitacaoAnaliseSofia solicitacao,
        string codigo,
        string mensagem) =>
        new()
        {
            VersaoContrato = SofiaContractValidator.ContractVersion,
            IdExecucao = solicitacao.IdExecucao,
            IdSolicitacao = solicitacao.IdSolicitacao,
            Status = "erro",
            Analise = "Não foi possível concluir a validação ECD.",
            Achados = [],
            Erro = new SofiaErro { Codigo = codigo, Mensagem = mensagem },
        };

    private sealed record DocumentAnalysisResult(
        Guid ExecutionId,
        ValidationExecution? Execution,
        IReadOnlyList<SofiaAchado> Achados,
        (string Codigo, string Mensagem)? Error)
    {
        public static DocumentAnalysisResult Succeeded(
            Guid executionId,
            ValidationExecution execution,
            IReadOnlyList<SofiaAchado> achados) =>
            new(executionId, execution, achados, null);

        public static DocumentAnalysisResult Failed(string codigo, string mensagem) =>
            new(Guid.Empty, null, [], (codigo, mensagem));
    }
}
