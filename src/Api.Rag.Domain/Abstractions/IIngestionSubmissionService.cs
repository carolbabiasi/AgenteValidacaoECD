namespace Api.Rag.Domain.Abstractions;

public sealed record IngestionSubmissionResult(
    Guid IngestionJobId,
    Guid ManualDocumentId,
    string StatusUrl);

public interface IIngestionSubmissionService
{
    Task<IngestionSubmissionResult> SubmitAsync(
        Stream fileContent,
        string fileName,
        string? contentType,
        string manualVersion,
        DateOnly? publicationDate,
        string layoutFamily,
        bool activateOnSuccess,
        CancellationToken cancellationToken = default);
}
