namespace Api.Rag.Domain.Abstractions;

public interface IRagManualBlobStore
{
    Task<string> SaveAsync(
        Guid manualDocumentId,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        Guid manualDocumentId,
        CancellationToken cancellationToken = default);
}
