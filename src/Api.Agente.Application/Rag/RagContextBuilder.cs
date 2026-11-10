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
            var manualVersion = ResolveManualVersion(versions, execution.Id, limitations);

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

    private string? ResolveManualVersion(
        IReadOnlyList<RagManualVersionInfo> versions,
        Guid executionId,
        List<string> limitations)
    {
        var manual = versions
            .FirstOrDefault(v => string.Equals(v.DocumentKind, "Manual", StringComparison.OrdinalIgnoreCase));

        if (manual is not null)
        {
            return manual.ManualVersion;
        }

        var fallback = versions.FirstOrDefault();
        if (fallback is not null)
        {
            logger.LogWarning(
                "Nenhuma versão ativa com DocumentKind=Manual para execução {ExecutionId}; usando {ManualVersion} ({DocumentKind}) como referência",
                executionId,
                fallback.ManualVersion,
                fallback.DocumentKind ?? "desconhecido");

            limitations.Add(
                "MANUAL_VERSION_KIND_MISMATCH: versão de referência "
                + fallback.ManualVersion
                + " não está classificada como Manual (tipo: "
                + (fallback.DocumentKind ?? "desconhecido")
                + ").");

            return fallback.ManualVersion;
        }

        logger.LogWarning(
            "A RAG não retornou nenhuma versão ativa de manual para execução {ExecutionId}",
            executionId);

        limitations.Add(
            "MANUAL_VERSION_UNKNOWN: versão do manual ECD não identificada; referências ao manual podem estar incompletas.");

        return null;
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
