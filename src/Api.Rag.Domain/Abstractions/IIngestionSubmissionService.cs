using Api.Rag.Domain.Enums;

namespace Api.Rag.Domain.Abstractions;

public sealed record IngestionSubmissionResult(
    Guid IngestionJobId,
    Guid ManualDocumentId,
    string StatusUrl);

public sealed record IngestionSubmissionRequest(
    string FileName,
    string? ContentType,
    string ManualVersion,
    DateOnly? PublicationDate,
    string LayoutFamily,
    bool ActivateOnSuccess,
    DocumentKind DocumentKind = DocumentKind.Manual,
    string? DocumentCode = null);

public interface IIngestionSubmissionService
{
    Task<IngestionSubmissionResult> SubmitAsync(
        Stream fileContent,
        IngestionSubmissionRequest request,
        CancellationToken cancellationToken = default);
}
