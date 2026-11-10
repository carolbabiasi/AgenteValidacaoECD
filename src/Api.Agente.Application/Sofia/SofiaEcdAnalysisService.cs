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
    private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(3);

    public async Task<ResultadoAnaliseSofia> AnalyzeAsync(
        SolicitacaoAnaliseSofia solicitacao,
        CancellationToken cancellationToken = default)
    {
        var doc = solicitacao.Documentos[0];
        var fileName = doc.Nome!.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
            ? doc.Nome
            : $"{doc.Nome}.txt";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(doc.Texto!));
        var userId = $"sofia:{solicitacao.IdExecucao:N}";
        if (userId.Length > 64)
        {
            userId = userId[..64];
        }

        ValidationSubmissionResult submitted;
        try
        {
            submitted = await submission.SubmitAsync(
                stream,
                fileName,
                "text/plain",
                userId,
                solicitacao.IdSolicitacao.ToString("N"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao submeter ECD para SOFIA {IdSolicitacao}", solicitacao.IdSolicitacao);
            return ErrorResult(solicitacao, "SUBMISSAO_ECD", ex.Message);
        }

        var deadline = DateTimeOffset.UtcNow.Add(MaxWait);
        ValidationExecution? execution = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            execution = await executions.GetWithFindingsAsync(submitted.ExecutionId, cancellationToken);
            if (execution?.Status is ValidationStatus.Completed or ValidationStatus.Failed or ValidationStatus.Cancelled)
            {
                break;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        if (execution is null)
        {
            return ErrorResult(
                solicitacao,
                "TIMEOUT_VALIDACAO",
                $"Validação ECD não concluiu a tempo (executionId={submitted.ExecutionId}).");
        }

        if (execution.Status == ValidationStatus.Failed)
        {
            return ErrorResult(
                solicitacao,
                execution.ErrorCode ?? "VALIDACAO_FALHOU",
                execution.ErrorMessage ?? "Pipeline ECD retornou falha.");
        }

        if (execution.Status == ValidationStatus.Cancelled)
        {
            return ErrorResult(solicitacao, "VALIDACAO_CANCELADA", "Validação ECD cancelada.");
        }

        if (execution.Status != ValidationStatus.Completed)
        {
            return ErrorResult(
                solicitacao,
                "TIMEOUT_VALIDACAO",
                $"Validação ECD não concluiu a tempo (executionId={submitted.ExecutionId}, status={execution.Status}).");
        }

        var achados = execution.Findings
            .Select((f, index) => MapFinding(f, doc.IdDocumento!, index))
            .ToList();

        var analise = achados.Count == 0
            ? "Nenhuma inconsistência detectada nas regras ativas do catálogo."
            : $"Foram identificadas {achados.Count} inconsistência(s) na validação ECD.";

        var observacoes = new List<string>
        {
            $"executionId={submitted.ExecutionId}",
            $"layout={execution.DetectedLayoutVersion}",
            $"manual={execution.ManualVersionUsed}",
        };

        AppendFirstRagReference(execution.ReportJson, observacoes);

        return new ResultadoAnaliseSofia
        {
            VersaoContrato = SofiaContractValidator.ContractVersion,
            IdExecucao = solicitacao.IdExecucao,
            IdSolicitacao = solicitacao.IdSolicitacao,
            Status = achados.Count == 0 ? "concluido" : "parcial",
            Analise = analise,
            Achados = achados,
            Observacoes = observacoes,
        };
    }

    private static void AppendFirstRagReference(string? reportJson, List<string> observacoes)
    {
        if (string.IsNullOrWhiteSpace(reportJson))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(reportJson);
            if (!document.RootElement.TryGetProperty("findings", out var findings))
            {
                return;
            }

            foreach (var finding in findings.EnumerateArray())
            {
                if (!finding.TryGetProperty("ragReferences", out var references))
                {
                    continue;
                }

                foreach (var reference in references.EnumerateArray())
                {
                    var manual = reference.TryGetProperty("manualVersion", out var mv)
                        ? mv.GetString()
                        : null;
                    var path = reference.TryGetProperty("hierarchyPath", out var hp)
                        ? hp.GetString()
                        : null;

                    if (string.IsNullOrWhiteSpace(manual))
                    {
                        continue;
                    }

                    observacoes.Add(string.IsNullOrWhiteSpace(path)
                        ? $"rag={manual}"
                        : $"rag={manual}:{path}");
                    return;
                }
            }
        }
        catch (JsonException)
        {
            // relatório ainda não enriquecido ou JSON inesperado
        }
    }

    private static SofiaAchado MapFinding(Domain.Entities.Finding finding, string documentoId, int index) =>
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
}
