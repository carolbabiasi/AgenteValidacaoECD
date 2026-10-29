using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Entities;
using Api.Rag.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace Api.Rag.Infrastructure.Persistence;

public sealed class RagIngestionRepository(RagDbContext db) : IRagIngestionRepository
{
    public async Task AddManualAndJobAsync(
        ManualDocument manual,
        IngestionJob job,
        bool activateOnSuccess,
        CancellationToken cancellationToken = default)
    {
        job.ActivateOnSuccess = activateOnSuccess;
        db.ManualDocuments.Add(manual);
        db.IngestionJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<IngestionJob?> GetJobWithManualAsync(
        Guid ingestionJobId,
        CancellationToken cancellationToken = default) =>
        db.IngestionJobs
            .Include(j => j.ManualDocument)
            .FirstOrDefaultAsync(j => j.Id == ingestionJobId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<HashSet<string>> GetExistingChunkKeysAsync(
        Guid manualDocumentId,
        CancellationToken cancellationToken = default)
    {
        var keys = await db.DocumentChunks
            .AsNoTracking()
            .Where(c => c.ManualDocumentId == manualDocumentId)
            .Select(c => c.ChunkKey)
            .ToListAsync(cancellationToken);

        return keys.ToHashSet(StringComparer.Ordinal);
    }

    public async Task AddChunksAsync(
        IEnumerable<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        db.DocumentChunks.AddRange(chunks);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveActiveManualsExceptAsync(
        string layoutFamily,
        Guid keepManualDocumentId,
        CancellationToken cancellationToken = default)
    {
        await db.ManualDocuments
            .Where(m => m.LayoutFamily == layoutFamily &&
                        m.Status == ManualDocumentStatus.Active &&
                        m.Id != keepManualDocumentId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(m => m.Status, ManualDocumentStatus.Archived),
                cancellationToken);
    }

    public Task<ManualDocument?> GetManualAsync(
        Guid manualDocumentId,
        CancellationToken cancellationToken = default) =>
        db.ManualDocuments.FirstOrDefaultAsync(m => m.Id == manualDocumentId, cancellationToken);

    public Task<ManualDocument?> GetManualWithChunksAsync(
        Guid manualDocumentId,
        CancellationToken cancellationToken = default) =>
        db.ManualDocuments
            .Include(m => m.Chunks)
            .FirstOrDefaultAsync(m => m.Id == manualDocumentId, cancellationToken);

    public async Task<IReadOnlyList<ManualDocument>> ListManualsAsync(
        string? layoutFamily,
        CancellationToken cancellationToken = default)
    {
        var query = db.ManualDocuments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(layoutFamily))
        {
            query = query.Where(m => m.LayoutFamily == layoutFamily);
        }

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<DocumentChunk?> GetChunkWithManualAsync(
        Guid chunkId,
        CancellationToken cancellationToken = default) =>
        db.DocumentChunks
            .AsNoTracking()
            .Include(c => c.ManualDocument)
            .FirstOrDefaultAsync(c => c.Id == chunkId, cancellationToken);

    public async Task<IReadOnlyList<(DocumentChunk Chunk, double Score)>> HybridSearchAsync(
        float[] queryEmbedding,
        string queryText,
        string? layoutFamily,
        string? manualVersion,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default)
    {
        var vector = new Vector(queryEmbedding);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var hybridResults = await ExecuteHybridSqlAsync(
                connection,
                vector,
                queryText,
                layoutFamily,
                manualVersion,
                topK,
                minScore,
                cancellationToken);

            if (hybridResults.Count > 0)
            {
                return hybridResults;
            }
        }
        catch (PostgresException ex) when (ex.SqlState is "42703")
        {
            // SearchVector ainda não migrado — segue para fallback vetorial.
        }

        return await FallbackVectorSearchAsync(
            queryEmbedding,
            layoutFamily,
            manualVersion,
            topK,
            minScore,
            cancellationToken);
    }

    private async Task<IReadOnlyList<(DocumentChunk Chunk, double Score)>> ExecuteHybridSqlAsync(
        NpgsqlConnection connection,
        Vector vector,
        string queryText,
        string? layoutFamily,
        string? manualVersion,
        int topK,
        double minScore,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c."Id",
                   c."ManualDocumentId",
                   c."ChunkKey",
                   c."ContentHash",
                   c."HierarchyPath",
                   c."SourceLocation",
                   c."Content",
                   (
                       CASE WHEN c."Embedding" IS NULL THEN 0
                            ELSE (1 - (c."Embedding" <=> @queryEmbedding)) * 0.6
                       END
                       +
                       CASE WHEN @queryText = '' THEN 0
                            ELSE ts_rank(c."SearchVector", plainto_tsquery('portuguese', @queryText)) * 0.4
                       END
                   ) AS score
            FROM rag."DocumentChunks" c
            INNER JOIN rag."ManualDocuments" m ON m."Id" = c."ManualDocumentId"
            WHERE (@layoutFamily IS NULL OR m."LayoutFamily" = @layoutFamily)
              AND (@manualVersion IS NULL OR m."ManualVersion" = @manualVersion)
              AND (
                    @manualVersion IS NOT NULL
                    OR m."Status" = @activeStatus
                  )
            ORDER BY score DESC
            LIMIT @topK
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("queryEmbedding", vector);
        command.Parameters.AddWithValue("queryText", queryText);
        command.Parameters.AddWithValue("layoutFamily", (object?)layoutFamily ?? DBNull.Value);
        command.Parameters.AddWithValue("manualVersion", (object?)manualVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("activeStatus", (int)ManualDocumentStatus.Active);
        command.Parameters.AddWithValue("topK", topK);

        var results = new List<(DocumentChunk Chunk, double Score)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var score = reader.GetDouble(8);
            if (score < minScore)
            {
                continue;
            }

            var chunk = new DocumentChunk
            {
                Id = reader.GetGuid(0),
                ManualDocumentId = reader.GetGuid(1),
                ChunkKey = reader.GetString(2),
                ContentHash = reader.GetString(3),
                HierarchyPath = reader.GetString(4),
                SourceLocation = reader.IsDBNull(5) ? null : reader.GetString(5),
                Content = reader.GetString(6),
            };

            results.Add((chunk, score));
        }

        if (results.Count == 0)
        {
            return results;
        }

        var manualIds = results.Select(r => r.Chunk.ManualDocumentId).Distinct().ToList();
        var manuals = await db.ManualDocuments
            .AsNoTracking()
            .Where(m => manualIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        for (var i = 0; i < results.Count; i++)
        {
            var entry = results[i];
            if (manuals.TryGetValue(entry.Chunk.ManualDocumentId, out var manual))
            {
                entry.Chunk.ManualDocument = manual;
            }

            results[i] = entry;
        }

        return results;
    }

    private async Task<IReadOnlyList<(DocumentChunk Chunk, double Score)>> FallbackVectorSearchAsync(
        float[] queryEmbedding,
        string? layoutFamily,
        string? manualVersion,
        int topK,
        double minScore,
        CancellationToken cancellationToken)
    {
        var chunksQuery = db.DocumentChunks
            .AsNoTracking()
            .Include(c => c.ManualDocument)
            .Where(c => c.Embedding != null);

        if (!string.IsNullOrWhiteSpace(layoutFamily))
        {
            chunksQuery = chunksQuery.Where(c => c.ManualDocument!.LayoutFamily == layoutFamily);
        }

        if (!string.IsNullOrWhiteSpace(manualVersion))
        {
            chunksQuery = chunksQuery.Where(c => c.ManualDocument!.ManualVersion == manualVersion);
        }
        else
        {
            chunksQuery = chunksQuery.Where(c => c.ManualDocument!.Status == ManualDocumentStatus.Active);
        }

        var candidates = await chunksQuery.Take(512).ToListAsync(cancellationToken);
        return candidates
            .Select(c => (Chunk: c, Score: CosineSimilarity(c.Embedding!, queryEmbedding)))
            .Where(x => x.Score >= minScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => (x.Chunk, x.Score))
            .ToList();
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length != right.Length)
        {
            return 0;
        }

        double dot = 0;
        double normLeft = 0;
        double normRight = 0;
        for (var i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            normLeft += left[i] * left[i];
            normRight += right[i] * right[i];
        }

        if (normLeft == 0 || normRight == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(normLeft) * Math.Sqrt(normRight));
    }
}
