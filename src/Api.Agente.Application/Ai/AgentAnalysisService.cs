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

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var reportJson = useLlm
                    ? await RunLlmEnrichmentAsync(execution, deterministicReportJson, ragContext, attempt, cancellationToken)
                    : RunDeterministicEnrichment(deterministicReportJson, ragContext);

                schemaValidator.Validate(reportJson);

                if (useLlm)
                {
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
                }

                return new AgentAnalysisResult(
                    reportJson,
                    ragContext.ManualVersionResolved,
                    snapshotJson,
                    attempt);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Tentativa {Attempt} de enriquecimento LLM falhou", attempt);
                if (attempt == MaxAttempts)
                {
                    break;
                }
            }
        }

        var fallback = RunDeterministicEnrichment(deterministicReportJson, ragContext);
        schemaValidator.Validate(fallback);
        return new AgentAnalysisResult(fallback, ragContext.ManualVersionResolved, snapshotJson, MaxAttempts);
    }

    private async Task<string> RunLlmEnrichmentAsync(
        ValidationExecution execution,
        string deterministicReportJson,
        RagContextSnapshot ragContext,
        int attempt,
        CancellationToken cancellationToken)
    {
        var kernel = BuildKernel();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var prompt = $"""
            Você enriquece um relatório JSON de validação ECD já produzido deterministicamente.
            Retorne APENAS JSON válido conforme schema 1.0.0 (mesmas chaves obrigatórias).
            Não invente achados novos; pode melhorar narrative/description e adicionar ragReferences usando evidências fornecidas.
            executionId={execution.Id}

            Relatório base:
            {deterministicReportJson}

            Evidências RAG:
            {JsonSerializer.Serialize(ragContext.EvidenceHits.Take(5))}
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
        var content = response.Content ?? throw new InvalidOperationException("LLM retornou conteúdo vazio.");
        return content.Trim();
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

    private static string RunDeterministicEnrichment(string deterministicReportJson, RagContextSnapshot ragContext)
    {
        var node = JsonNode.Parse(deterministicReportJson)
            ?? throw new InvalidOperationException("Relatório determinístico inválido.");

        var limitations = node["limitations"]?.AsArray() ?? new JsonArray();
        foreach (var item in ragContext.Limitations)
        {
            limitations.Add(item);
        }

        limitations.Add("Enriquecimento LLM não executado (modo determinístico/homolog).");
        node["limitations"] = limitations;

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
