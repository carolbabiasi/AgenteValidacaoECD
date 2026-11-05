using System.ComponentModel;
using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Enums;
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
        [Description("Texto da consulta.")] string query,
        [Description("Família/versão de leiaute para filtro (ex.: ECD).")] string? layoutVersion = null,
        [Description("Versão do manual; nula usa a ativa.")] string? manualVersion = null,
        [Description("Manual, Faq ou Norm; nulo busca em todos os tipos ativos.")] string? documentKind = null,
        [Description("Quantidade máxima de resultados.")] int? topK = null,
        [Description("Score mínimo (0..1).")] double? minScore = null,
        CancellationToken cancellationToken = default)
    {
        DocumentKind? kind = null;
        if (!string.IsNullOrWhiteSpace(documentKind))
        {
            if (!Enum.TryParse<DocumentKind>(documentKind.Trim(), ignoreCase: true, out var parsed) ||
                !Enum.IsDefined(parsed))
            {
                throw new ArgumentException("documentKind inválido; use Manual, Faq ou Norm.");
            }

            kind = parsed;
        }

        var response = await retrieval.SearchManualAsync(
            new ManualSearchRequest(
                query,
                layoutVersion,
                manualVersion,
                topK ?? 8,
                minScore ?? 0.0,
                kind),
            cancellationToken);

        return new SearchManualOutput
        {
            Results = response.Results.Select(r => new SearchManualResultItem
            {
                ChunkId = r.ChunkId,
                ManualVersion = r.ManualVersion,
                DocumentKind = r.DocumentKind.ToString(),
                DocumentCode = r.DocumentCode,
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
        [Description("Identificador do chunk.")] Guid chunkId,
        CancellationToken cancellationToken = default)
    {
        var hit = await retrieval.GetChunkAsync(chunkId, cancellationToken);
        if (hit is null)
        {
            return null;
        }

        return new GetChunkOutput
        {
            ChunkId = hit.ChunkId,
            ManualVersion = hit.ManualVersion,
            DocumentKind = hit.DocumentKind.ToString(),
            DocumentCode = hit.DocumentCode,
            HierarchyPath = hit.HierarchyPath,
            SourceLocation = hit.SourceLocation,
            Content = hit.Excerpt,
        };
    }

    [McpServerTool(Name = "rag.list_manual_versions", ReadOnly = true)]
    [Description("Lista versões de manual disponíveis por família de leiaute.")]
    public async Task<ListManualVersionsOutput> ListManualVersionsAsync(
        [Description("Família de leiaute (ex.: ECD).")] string? layoutFamily = null,
        [Description("Somente versões ativas.")] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var items = await retrieval.ListManualVersionsAsync(
            layoutFamily,
            activeOnly ?? true,
            documentKind: null,
            cancellationToken);

        return new ListManualVersionsOutput
        {
            Items = items.Select(i => new ManualVersionItem
            {
                ManualVersion = i.ManualVersion,
                PublicationDate = i.PublicationDate?.ToString("yyyy-MM-dd"),
                Status = i.Status,
                DocumentKind = i.DocumentKind.ToString(),
                DocumentCode = i.DocumentCode,
            }).ToList(),
        };
    }
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

    public required string DocumentKind { get; init; }

    public required string DocumentCode { get; init; }

    public required string HierarchyPath { get; init; }

    public string? SourceLocation { get; init; }

    public required string Excerpt { get; init; }

    public double Score { get; init; }
}

public sealed class GetChunkOutput
{
    public Guid ChunkId { get; init; }

    public required string ManualVersion { get; init; }

    public required string DocumentKind { get; init; }

    public required string DocumentCode { get; init; }

    public required string HierarchyPath { get; init; }

    public string? SourceLocation { get; init; }

    public required string Content { get; init; }
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

    public required string DocumentKind { get; init; }

    public required string DocumentCode { get; init; }
}
