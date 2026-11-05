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
        IngestionSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("Nome de arquivo obrigatório.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ManualVersion))
        {
            throw new ArgumentException("manualVersion obrigatório.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.LayoutFamily))
        {
            throw new ArgumentException("layoutFamily obrigatório.", nameof(request));
        }

        var documentCode = ResolveDocumentCode(request.DocumentKind, request.DocumentCode);

        var manualId = Guid.NewGuid();
        var sourceUri = await blobStore.SaveAsync(manualId, fileContent, request.FileName, cancellationToken);

        var manual = new ManualDocument
        {
            Id = manualId,
            FileName = request.FileName,
            ManualVersion = request.ManualVersion.Trim(),
            PublicationDate = request.PublicationDate,
            LayoutFamily = request.LayoutFamily.Trim(),
            DocumentKind = request.DocumentKind,
            DocumentCode = documentCode,
            Status = ManualDocumentStatus.Draft,
            SourceUri = sourceUri,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var job = new IngestionJob
        {
            Id = Guid.NewGuid(),
            ManualDocumentId = manualId,
            Status = IngestionJobStatus.Pending,
            ActivateOnSuccess = request.ActivateOnSuccess,
        };

        manual.IngestionJobs.Add(job);
        await repository.AddManualAndJobAsync(manual, job, request.ActivateOnSuccess, cancellationToken);

        return new IngestionSubmissionResult(
            job.Id,
            manualId,
            $"/api/v1/ingestions/{job.Id}");
    }

    private static string ResolveDocumentCode(DocumentKind kind, string? documentCode)
    {
        if (string.IsNullOrWhiteSpace(documentCode))
        {
            return kind == DocumentKind.Manual
                ? ManualDocument.DefaultManualCode
                : throw new ArgumentException(
                    "documentCode obrigatório para documentos que não são o Manual ECD.",
                    nameof(documentCode));
        }

        var normalized = documentCode.Trim().ToUpperInvariant();
        if (normalized.Length > ManualDocument.MaxDocumentCodeLength)
        {
            throw new ArgumentException(
                $"documentCode deve ter no máximo {ManualDocument.MaxDocumentCodeLength} caracteres.",
                nameof(documentCode));
        }

        return normalized;
    }
}
