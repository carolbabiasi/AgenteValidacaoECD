using Api.Rag.Domain.Entities;

namespace Api.Rag.Domain.Abstractions;

public interface IRagIngestionRepository
{
    Task AddManualAndJobAsync(
        ManualDocument manual,
        IngestionJob job,
        bool activateOnSuccess,
        CancellationToken cancellationToken = default);

    Task<IngestionJob?> GetJobWithManualAsync(Guid ingestionJobId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetExistingChunkKeysAsync(
        Guid manualDocumentId,
        CancellationToken cancellationToken = default);

    Task AddChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    Task ArchiveActiveManualsExceptAsync(
        string layoutFamily,
        Guid keepManualDocumentId,
        CancellationToken cancellationToken = default);

    Task<ManualDocument?> GetManualAsync(Guid manualDocumentId, CancellationToken cancellationToken = default);

    Task<ManualDocument?> GetManualWithChunksAsync(
        Guid manualDocumentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManualDocument>> ListManualsAsync(
        string? layoutFamily,
        CancellationToken cancellationToken = default);

    Task<DocumentChunk?> GetChunkWithManualAsync(
        Guid chunkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(DocumentChunk Chunk, double Score)>> HybridSearchAsync(
        float[] queryEmbedding,
        string queryText,
        string? layoutFamily,
        string? manualVersion,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default);
}
