using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Application.Rag;

public sealed class RagContextBuilder(
    IRagMcpClient ragClient,
    ILogger<RagContextBuilder> logger) : IRagContextBuilder
{
    public async Task<RagContextSnapshot> BuildAsync(
        ValidationExecution execution,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default)
    {
        var layoutFamily = ResolveLayoutFamily(execution.DetectedLayoutVersion);
        var limitations = new List<string>();
        var hits = new List<RagManualSearchHit>();

        try
        {
            var versions = await ragClient.ListManualVersionsAsync(layoutFamily, activeOnly: true, cancellationToken);
            var manualVersion = versions
                .FirstOrDefault(v => string.Equals(v.DocumentKind, "Manual", StringComparison.OrdinalIgnoreCase))
                ?.ManualVersion;

            var query = BuildQuery(findings);
            if (string.IsNullOrWhiteSpace(query))
            {
                query = "validação ECD registros obrigatórios leiaute";
            }

            var search = await ragClient.SearchManualAsync(
                query,
                layoutFamily,
                manualVersion: null,
                topK: 6,
                cancellationToken);

            hits.AddRange(search.Results);

            return new RagContextSnapshot(
                manualVersion,
                layoutFamily,
                hits,
                limitations);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao consultar RAG via MCP para execução {ExecutionId}", execution.Id);
            limitations.Add("RAG_UNAVAILABLE: manual ECD não consultado nesta execução.");
            return new RagContextSnapshot(null, layoutFamily, hits, limitations);
        }
    }

    private static string ResolveLayoutFamily(string? detectedLayoutVersion) =>
        string.IsNullOrWhiteSpace(detectedLayoutVersion) ? "ECD" : "ECD";

    private static string BuildQuery(IReadOnlyList<Finding> findings)
    {
        if (findings.Count == 0)
        {
            return string.Empty;
        }

        var parts = findings
            .Take(3)
            .Select(f => $"{f.PrimaryRecordType} {f.Summary}")
            .Where(s => !string.IsNullOrWhiteSpace(s));

        return string.Join("; ", parts);
    }
}
