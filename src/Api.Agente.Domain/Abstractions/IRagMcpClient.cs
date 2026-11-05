namespace Api.Agente.Domain.Abstractions;

public sealed record RagManualSearchHit(
    Guid ChunkId,
    string ManualVersion,
    string HierarchyPath,
    string? SourceLocation,
    string Excerpt,
    double Score);

public sealed record RagManualSearchResponse(
    IReadOnlyList<RagManualSearchHit> Results,
    string RetrievalMode);

public sealed record RagManualVersionInfo(
    string ManualVersion,
    DateOnly? PublicationDate,
    string Status,
    string? DocumentKind = null,
    string? DocumentCode = null);

public interface IRagMcpClient
{
    Task<RagManualSearchResponse> SearchManualAsync(
        string query,
        string? layoutVersion,
        string? manualVersion,
        int topK,
        CancellationToken cancellationToken = default);

    Task<RagManualSearchHit?> GetChunkAsync(
        Guid chunkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RagManualVersionInfo>> ListManualVersionsAsync(
        string? layoutFamily,
        bool activeOnly,
        CancellationToken cancellationToken = default);
}
