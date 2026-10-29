using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Entities;
using Api.Rag.Domain.Enums;

namespace Api.Rag.Application.Ingestion;

public sealed class IngestionSubmissionService(
    IRagIngestionRepository repository,
    IRagManualBlobStore blobStore) : IIngestionSubmissionService
{
    public async Task<IngestionSubmissionResult> SubmitAsync(
        Stream fileContent,
        string fileName,
        string? contentType,
        string manualVersion,
        DateOnly? publicationDate,
        string layoutFamily,
        bool activateOnSuccess,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Nome de arquivo obrigatório.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(manualVersion))
        {
            throw new ArgumentException("manualVersion obrigatório.", nameof(manualVersion));
        }

        if (string.IsNullOrWhiteSpace(layoutFamily))
        {
            throw new ArgumentException("layoutFamily obrigatório.", nameof(layoutFamily));
        }

        var manualId = Guid.NewGuid();
        var sourceUri = await blobStore.SaveAsync(manualId, fileContent, fileName, cancellationToken);

        var manual = new ManualDocument
        {
            Id = manualId,
            FileName = fileName,
            ManualVersion = manualVersion.Trim(),
            PublicationDate = publicationDate,
            LayoutFamily = layoutFamily.Trim(),
            Status = ManualDocumentStatus.Draft,
            SourceUri = sourceUri,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var job = new IngestionJob
        {
            Id = Guid.NewGuid(),
            ManualDocumentId = manualId,
            Status = IngestionJobStatus.Pending,
            ActivateOnSuccess = activateOnSuccess,
        };

        manual.IngestionJobs.Add(job);
        await repository.AddManualAndJobAsync(manual, job, activateOnSuccess, cancellationToken);

        return new IngestionSubmissionResult(
            job.Id,
            manualId,
            $"/api/v1/ingestions/{job.Id}");
    }
}
