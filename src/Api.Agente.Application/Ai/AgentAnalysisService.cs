using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Api.Agente.Application.Ai;

public sealed class AgentAnalysisService(
    IAgentResponseSchemaValidator schemaValidator,
    IExecutionAuthorization authorization,
    ILlmUsageRecorder usageRecorder,
    IConfiguration configuration,
    ILogger<AgentAnalysisService> logger) : IAgentAnalysisService
{
    private const int MaxAttempts = 3;

    private const string NotConfiguredLimitation =
        "Enriquecimento LLM não executado (OpenAI não configurado; modo determinístico/homolog).";

    private static readonly string FailedLimitation =
        $"Enriquecimento LLM falhou após {MaxAttempts} tentativas; relatório determinístico preservado.";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<AgentAnalysisResult> EnrichReportAsync(
        ValidationExecution execution,
        string deterministicReportJson,
        RagContextSnapshot ragContext,
        CancellationToken cancellationToken = default)
    {
        await authorization.AuthorizeAgentOperationsAsync(execution.Id, cancellationToken);

        var snapshotJson = JsonSerializer.Serialize(new
        {
            manualVersion = ragContext.ManualVersionResolved,
            layoutFamily = ragContext.LayoutFamily,
            hits = ragContext.EvidenceHits.Select(h => new
            {
                chunkId = h.ChunkId,
                manualVersion = h.ManualVersion,
                hierarchyPath = h.HierarchyPath,
                excerpt = h.Excerpt,
                score = h.Score,
            }),
            limitations = ragContext.Limitations,
        }, JsonOptions);

        var apiKey = configuration["OpenAI:ApiKey"];
        var useLlm = !string.IsNullOrWhiteSpace(apiKey) &&
                     !string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Testing", StringComparison.OrdinalIgnoreCase);

        if (!useLlm)
        {
            return Deterministic(NotConfiguredLimitation, attempts: 1);
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var reportJson = await RunLlmEnrichmentAsync(
                    execution,
                    deterministicReportJson,
                    ragContext,
                    cancellationToken);

                schemaValidator.Validate(reportJson);

                await usageRecorder.RecordAsync(new LlmUsageEvent(
                    execution.Id,
                    execution.CorrelationId,
                    attempt,
                    "agent_analysis",
                    configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini",
                    "Succeeded",
                    PromptTokens: null,
                    CompletionTokens: null,
                    DurationMs: 0,
                    EstimatedCostUsd: null), cancellationToken);

                return new AgentAnalysisResult(
                    reportJson,
                    ragContext.ManualVersionResolved,
                    snapshotJson,
                    attempt);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Tentativa {Attempt} de enriquecimento LLM falhou", attempt);
            }
        }

        logger.LogWarning(
            "Enriquecimento LLM falhou nas {MaxAttempts} tentativas da execução {ExecutionId}; usando relatório determinístico",
            MaxAttempts,
            execution.Id);

        return Deterministic(FailedLimitation, MaxAttempts);

        AgentAnalysisResult Deterministic(string limitation, int attempts)
        {
            var reportJson = RunDeterministicEnrichment(deterministicReportJson, ragContext, limitation);
            schemaValidator.Validate(reportJson);

            return new AgentAnalysisResult(reportJson, ragContext.ManualVersionResolved, snapshotJson, attempts);
        }
    }

    /// <summary>
    /// Pede ao LLM apenas a camada de explicação e aplica esse enxerto sobre o relatório
    /// determinístico. O modelo nunca devolve o relatório inteiro: além de P3 (resultado de
    /// regra não pode ser reescrito por LLM), a resposta livre violava o schema — vinha com
    /// "ragReferences" na raiz, onde additionalProperties é false.
    /// </summary>
    private async Task<string> RunLlmEnrichmentAsync(
        ValidationExecution execution,
        string deterministicReportJson,
        RagContextSnapshot ragContext,
        CancellationToken cancellationToken)
    {
        var payload = await RequestEnrichmentAsync(execution, deterministicReportJson, ragContext, cancellationToken);

        return ApplyEnrichment(deterministicReportJson, payload, ragContext);
    }

    private async Task<JsonObject> RequestEnrichmentAsync(
        ValidationExecution execution,
        string deterministicReportJson,
        RagContextSnapshot ragContext,
        CancellationToken cancellationToken)
    {
        var kernel = BuildKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var evidence = JsonSerializer.Serialize(
            ragContext.EvidenceHits.Take(5).Select(h => new
            {
                chunkId = h.ChunkId,
                manualVersion = h.ManualVersion,
                hierarchyPath = h.HierarchyPath,
                excerpt = h.Excerpt,
            }),
            JsonOptions);

        var prompt = $$"""
            Você explica um relatório de validação ECD já produzido deterministicamente.
            Não repita o relatório e não altere achados: devolva SOMENTE o enxerto abaixo.

            Formato exato da resposta:
            {
              "narrative": "texto curto em português explicando o resultado",
              "findings": [
                {
                  "findingId": "id existente no relatório base",
                  "description": "explicação do achado em português",
                  "ragReferences": [ { "chunkId": "id de uma evidência fornecida", "manualVersion": "...", "hierarchyPath": "..." } ]
                }
              ]
            }

            Regras:
            - use apenas findingId que exista no relatório base; não crie achados;
            - em ragReferences use apenas chunkId presente nas evidências fornecidas;
            - sem evidência aplicável, devolva ragReferences vazio;
            - nenhuma outra chave além de narrative e findings.

            executionId={{execution.Id}}

            Relatório base:
            {{deterministicReportJson}}

            Evidências RAG:
            {{evidence}}
            """;

        var history = new ChatHistory();
        history.AddSystemMessage("Responda somente com JSON. Não inclua markdown.");
        history.AddUserMessage(prompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.2,
            MaxTokens = 4096,
            ResponseFormat = "json_object",
        };

        var response = await chat.GetChatMessageContentAsync(history, settings, kernel, cancellationToken);
        var content = response.Content?.Trim()
            ?? throw new InvalidOperationException("LLM retornou conteúdo vazio.");

        return JsonNode.Parse(content) as JsonObject
            ?? throw new InvalidOperationException("LLM_PAYLOAD_INVALID: resposta não é um objeto JSON.");
    }

    private static string ApplyEnrichment(
        string deterministicReportJson,
        JsonObject payload,
        RagContextSnapshot ragContext)
    {
        var node = JsonNode.Parse(deterministicReportJson) as JsonObject
            ?? throw new InvalidOperationException("Relatório determinístico inválido.");

        AppendRagLimitations(node, ragContext);

        var narrative = payload["narrative"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(narrative) && node["summary"] is JsonObject summary)
        {
            summary["narrative"] = narrative;
        }

        if (payload["findings"] is JsonArray enriched && node["findings"] is JsonArray findings)
        {
            MergeFindings(findings, enriched, ragContext);
        }

        return node.ToJsonString(JsonOptions);
    }

    private static void MergeFindings(JsonArray findings, JsonArray enriched, RagContextSnapshot ragContext)
    {
        var allowedChunks = ragContext.EvidenceHits
            .Select(h => h.ChunkId.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var byId = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var item in enriched.OfType<JsonObject>())
        {
            var id = item["findingId"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(id))
            {
                byId[id] = item;
            }
        }

        foreach (var finding in findings.OfType<JsonObject>())
        {
            var id = finding["findingId"]?.GetValue<string>();
            if (id is null || !byId.TryGetValue(id, out var item))
            {
                continue;
            }

            var description = item["description"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(description))
            {
                finding["description"] = description;
            }

            var references = BuildReferences(item["ragReferences"] as JsonArray, allowedChunks);
            if (references.Count > 0)
            {
                finding["ragReferences"] = references;
            }
        }
    }

    // Só entram citações de chunks que a RAG devolveu nesta execução: sem isso o modelo
    // pode inventar chunkId e o relatório passa a citar evidência que não existe.
    private static JsonArray BuildReferences(JsonArray? candidates, HashSet<string> allowedChunks)
    {
        var references = new JsonArray();
        if (candidates is null)
        {
            return references;
        }

        foreach (var candidate in candidates.OfType<JsonObject>())
        {
            var chunkId = candidate["chunkId"]?.GetValue<string>();
            var manualVersion = candidate["manualVersion"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(chunkId) ||
                string.IsNullOrWhiteSpace(manualVersion) ||
                !allowedChunks.Contains(chunkId))
            {
                continue;
            }

            var reference = new JsonObject
            {
                ["chunkId"] = chunkId,
                ["manualVersion"] = manualVersion,
            };

            var hierarchyPath = candidate["hierarchyPath"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(hierarchyPath))
            {
                reference["hierarchyPath"] = hierarchyPath;
            }

            references.Add(reference);
        }

        return references;
    }

    private Kernel BuildKernel()
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey não configurada.");
        var model = configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";

        return Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(model, apiKey)
            .Build();
    }

    private static void AppendRagLimitations(JsonObject node, RagContextSnapshot ragContext)
    {
        var limitations = node["limitations"]?.AsArray() ?? new JsonArray();
        foreach (var item in ragContext.Limitations)
        {
            limitations.Add(item);
        }

        node["limitations"] = limitations;
    }

    private static string RunDeterministicEnrichment(
        string deterministicReportJson,
        RagContextSnapshot ragContext,
        string limitation)
    {
        var node = JsonNode.Parse(deterministicReportJson) as JsonObject
            ?? throw new InvalidOperationException("Relatório determinístico inválido.");

        AppendRagLimitations(node, ragContext);
        node["limitations"]!.AsArray().Add(limitation);

        if (node["summary"] is JsonObject summary)
        {
            var narrative = summary["narrative"]?.GetValue<string>() ?? string.Empty;
            if (ragContext.EvidenceHits.Count > 0)
            {
                narrative += " Contexto do Manual ECD indexado foi anexado via RAG (modo determinístico).";
            }

            summary["narrative"] = narrative;
        }

        if (node["findings"] is JsonArray findings && findings.Count > 0 && ragContext.EvidenceHits.Count > 0)
        {
            var first = findings[0] as JsonObject;
            if (first is not null)
            {
                var refs = new JsonArray();
                foreach (var hit in ragContext.EvidenceHits.Take(3))
                {
                    refs.Add(new JsonObject
                    {
                        ["chunkId"] = hit.ChunkId.ToString(),
                        ["manualVersion"] = hit.ManualVersion,
                        ["hierarchyPath"] = hit.HierarchyPath,
                    });
                }

                first["ragReferences"] = refs;
            }
        }

        return node.ToJsonString(JsonOptions);
    }
}
