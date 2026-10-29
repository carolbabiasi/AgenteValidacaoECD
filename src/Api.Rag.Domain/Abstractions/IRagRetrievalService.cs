namespace Api.Rag.Domain.Abstractions;

public sealed record ManualSearchRequest(
    string Query,
    string? LayoutVersion,
    string? ManualVersion,
    int TopK = 8,
    double MinScore = 0.0);

public sealed record ManualSearchHit(
    Guid ChunkId,
    string ManualVersion,
    string HierarchyPath,
    string? SourceLocation,
    string Excerpt,
    double Score);

public sealed record ManualSearchResponse(
    IReadOnlyList<ManualSearchHit> Results,
    string RetrievalMode);

public sealed record ManualVersionSummary(
    string ManualVersion,
    DateOnly? PublicationDate,
    string Status,
    string LayoutFamily);

public interface IRagRetrievalService
{
    Task<ManualSearchResponse> SearchManualAsync(
        ManualSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ManualSearchHit?> GetChunkAsync(
        Guid chunkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManualVersionSummary>> ListManualVersionsAsync(
        string? layoutFamily,
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
