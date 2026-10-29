using System.ComponentModel;
using Api.Rag.Domain.Abstractions;
using Api.Rag.Host.Auth;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace Api.Rag.Host.Mcp;

[Authorize(Policy = RagAuthExtensions.RagReaderPolicy)]
[McpServerToolType]
public sealed class RagMcpTools(IRagRetrievalService retrieval)
{
    [McpServerTool(Name = "rag.search_manual", ReadOnly = true)]
    [Description("Busca híbrida no Manual ECD indexado (vetor + FTS).")]
    public async Task<SearchManualOutput> SearchManualAsync(
        SearchManualInput input,
        CancellationToken cancellationToken)
    {
        var response = await retrieval.SearchManualAsync(
            new ManualSearchRequest(
                input.Query,
                input.LayoutVersion,
                input.ManualVersion,
                input.TopK ?? 8,
                input.MinScore ?? 0.0),
            cancellationToken);

        return new SearchManualOutput
        {
            Results = response.Results.Select(r => new SearchManualResultItem
            {
                ChunkId = r.ChunkId,
                ManualVersion = r.ManualVersion,
                HierarchyPath = r.HierarchyPath,
                SourceLocation = r.SourceLocation,
                Excerpt = r.Excerpt,
                Score = r.Score,
            }).ToList(),
            RetrievalMode = response.RetrievalMode,
        };
    }

    [McpServerTool(Name = "rag.get_chunk", ReadOnly = true)]
    [Description("Recupera chunk completo para citação.")]
    public async Task<GetChunkOutput?> GetChunkAsync(
        GetChunkInput input,
        CancellationToken cancellationToken)
    {
        var hit = await retrieval.GetChunkAsync(input.ChunkId, cancellationToken);
        if (hit is null)
        {
            return null;
        }

        return new GetChunkOutput
        {
            ChunkId = hit.ChunkId,
            ManualVersion = hit.ManualVersion,
            HierarchyPath = hit.HierarchyPath,
            SourceLocation = hit.SourceLocation,
            Content = hit.Excerpt,
        };
    }

    [McpServerTool(Name = "rag.list_manual_versions", ReadOnly = true)]
    [Description("Lista versões de manual disponíveis por família de leiaute.")]
    public async Task<ListManualVersionsOutput> ListManualVersionsAsync(
        ListManualVersionsInput input,
        CancellationToken cancellationToken)
    {
        var items = await retrieval.ListManualVersionsAsync(
            input.LayoutFamily,
            input.ActiveOnly ?? true,
            cancellationToken);

        return new ListManualVersionsOutput
        {
            Items = items.Select(i => new ManualVersionItem
            {
                ManualVersion = i.ManualVersion,
                PublicationDate = i.PublicationDate?.ToString("yyyy-MM-dd"),
                Status = i.Status,
            }).ToList(),
        };
    }
}

public sealed class SearchManualInput
{
    public required string Query { get; init; }

    public string? LayoutVersion { get; init; }

    public string? ManualVersion { get; init; }

    public int? TopK { get; init; }

    public double? MinScore { get; init; }
}

public sealed class SearchManualOutput
{
    public required IList<SearchManualResultItem> Results { get; init; }

    public required string RetrievalMode { get; init; }
}

public sealed class SearchManualResultItem
{
    public Guid ChunkId { get; init; }

    public required string ManualVersion { get; init; }

    public required string HierarchyPath { get; init; }

    public string? SourceLocation { get; init; }

    public required string Excerpt { get; init; }

    public double Score { get; init; }
}

public sealed class GetChunkInput
{
    public Guid ChunkId { get; init; }
}

public sealed class GetChunkOutput
{
    public Guid ChunkId { get; init; }

    public required string ManualVersion { get; init; }

    public required string HierarchyPath { get; init; }

    public string? SourceLocation { get; init; }

    public required string Content { get; init; }
}

public sealed class ListManualVersionsInput
{
    public string? LayoutFamily { get; init; }

    public bool? ActiveOnly { get; init; }
}

public sealed class ListManualVersionsOutput
{
    public required IList<ManualVersionItem> Items { get; init; }
}

public sealed class ManualVersionItem
{
    public required string ManualVersion { get; init; }

    public string? PublicationDate { get; init; }

    public required string Status { get; init; }
}
