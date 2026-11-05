using Api.Rag.Application.Chunking;
using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Entities;
using Api.Rag.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Api.Rag.Application.Ingestion;

public sealed class ManualIngestionPipeline(
    IRagIngestionRepository repository,
    IRagManualBlobStore blobStore,
    IManualTextExtractor textExtractor,
    IEmbeddingService embeddingService,
    ILogger<ManualIngestionPipeline> logger) : IManualIngestionPipeline
{
    private const int EmbeddingBatchSize = 16;

    public async Task RunAsync(Guid ingestionJobId, CancellationToken cancellationToken = default)
    {
        var job = await repository.GetJobWithManualAsync(ingestionJobId, cancellationToken)
            ?? throw new InvalidOperationException($"IngestionJob {ingestionJobId} não encontrado.");

        var manual = job.ManualDocument
            ?? throw new InvalidOperationException("ManualDocument ausente no job de ingestão.");

        job.Status = IngestionJobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        job.ChunksCreated = 0;
        job.ChunksSkipped = 0;
        await repository.SaveChangesAsync(cancellationToken);

        try
        {
            await using var stream = await blobStore.OpenReadAsync(manual.Id, cancellationToken);
            var extracted = await textExtractor.ExtractAsync(
                stream,
                manual.FileName,
                contentType: null,
                cancellationToken);

            var drafts = StructuralChunker.Chunk(extracted.PlainText);
            var existingKeys = await repository.GetExistingChunkKeysAsync(manual.Id, cancellationToken);

            var pending = new List<(StructuralChunkDraft Draft, string ChunkKey, string ContentHash)>();
            foreach (var draft in drafts)
            {
                var hash = ChunkDeduplication.ComputeContentHash(draft.Content);
                var key = ChunkDeduplication.BuildChunkKey(draft.HierarchyPath, hash);
                if (existingKeys.Contains(key))
                {
                    job.ChunksSkipped++;
                    continue;
                }

                pending.Add((draft, key, hash));
                existingKeys.Add(key);
            }

            for (var i = 0; i < pending.Count; i += EmbeddingBatchSize)
            {
                var batch = pending.Skip(i).Take(EmbeddingBatchSize).ToList();
                var vectors = await embeddingService.EmbedDocumentsAsync(
                    batch.Select(b => b.Draft.Content).ToList(),
                    cancellationToken);

                if (vectors.Count != batch.Count)
                {
                    throw new InvalidOperationException("Provedor de embeddings retornou quantidade inesperada.");
                }

                var chunks = new List<DocumentChunk>();
                for (var j = 0; j < batch.Count; j++)
                {
                    var item = batch[j];
                    var vector = vectors[j];
                    if (vector.Length != embeddingService.Dimensions)
                    {
                        throw new InvalidOperationException(
                            $"Embedding dim={vector.Length}; esperado {embeddingService.Dimensions}.");
                    }

                    chunks.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        ManualDocumentId = manual.Id,
                        ChunkKey = item.ChunkKey,
                        ContentHash = item.ContentHash,
                        HierarchyPath = item.Draft.HierarchyPath,
                        SourceLocation = item.Draft.SourceLocation,
                        Content = item.Draft.Content,
                        Embedding = vector,
                    });
                }

                await repository.AddChunksAsync(chunks, cancellationToken);
                job.ChunksCreated += chunks.Count;
            }

            if (job.ChunksCreated == 0 && job.ChunksSkipped == 0)
            {
                throw new InvalidOperationException("Nenhum chunk produzido a partir do manual.");
            }

            if (job.ActivateOnSuccess)
            {
                await repository.ArchiveActiveManualsExceptAsync(
                    manual.LayoutFamily,
                    manual.DocumentCode,
                    manual.Id,
                    cancellationToken);
                manual.Status = ManualDocumentStatus.Active;
            }
            else
            {
                manual.Status = ManualDocumentStatus.Draft;
            }

            job.Status = IngestionJobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Ingestão {JobId} concluída: {Created} chunks, {Skipped} ignorados",
                ingestionJobId,
                job.ChunksCreated,
                job.ChunksSkipped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na ingestão {JobId}", ingestionJobId);
            job.Status = IngestionJobStatus.Failed;
            job.ErrorLog = ex.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            manual.Status = ManualDocumentStatus.Draft;
            await repository.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
