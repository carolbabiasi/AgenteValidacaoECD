using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Entities;
using Api.Rag.Domain.Enums;

namespace Api.Rag.Application.Retrieval;

public sealed class HybridManualSearch(
    IRagIngestionRepository repository,
    IEmbeddingService embeddingService) : IRagRetrievalService
{
    public async Task<ManualSearchResponse> SearchManualAsync(
        ManualSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new ManualSearchResponse([], "hybrid");
        }

        var queryEmbedding = await embeddingService.EmbedQueryAsync(request.Query, cancellationToken);
        var hits = await repository.HybridSearchAsync(
            queryEmbedding,
            request.Query,
            request.LayoutVersion,
            request.ManualVersion,
            request.DocumentKind,
            Math.Clamp(request.TopK, 1, 32),
            request.MinScore,
            cancellationToken);

        var results = hits
            .Select(h => ToHit(h.Chunk, TruncateExcerpt(h.Chunk.Content), h.Score))
            .ToList();

        return new ManualSearchResponse(results, "hybrid");
    }

    public async Task<ManualSearchHit?> GetChunkAsync(
        Guid chunkId,
        CancellationToken cancellationToken = default)
    {
        var chunk = await repository.GetChunkWithManualAsync(chunkId, cancellationToken);
        return chunk is null ? null : ToHit(chunk, chunk.Content, 1.0);
    }

    public async Task<IReadOnlyList<ManualVersionSummary>> ListManualVersionsAsync(
        string? layoutFamily,
        bool activeOnly,
        DocumentKind? documentKind = null,
        CancellationToken cancellationToken = default)
    {
        var manuals = await repository.ListManualsAsync(layoutFamily, cancellationToken);
        var filtered = manuals
            .Where(m => !activeOnly || m.Status == ManualDocumentStatus.Active)
            .Where(m => documentKind is null || m.DocumentKind == documentKind);

        return filtered
            .OrderByDescending(m => m.PublicationDate)
            .ThenByDescending(m => m.CreatedAt)
            .Select(m => new ManualVersionSummary(
                m.ManualVersion,
                m.PublicationDate,
                m.Status.ToString(),
                m.LayoutFamily,
                m.DocumentKind,
                m.DocumentCode))
            .ToList();
    }

    private static ManualSearchHit ToHit(DocumentChunk chunk, string excerpt, double score) =>
        new(
            chunk.Id,
            chunk.ManualDocument?.ManualVersion ?? string.Empty,
            chunk.HierarchyPath,
            chunk.SourceLocation,
            excerpt,
            score,
            chunk.ManualDocument?.DocumentKind ?? DocumentKind.Manual,
            chunk.ManualDocument?.DocumentCode ?? ManualDocument.DefaultManualCode);

    private static string TruncateExcerpt(string content, int maxLength = 480)
    {
        if (content.Length <= maxLength)
        {
            return content;
        }

        return content[..maxLength] + "...";
    }
}
