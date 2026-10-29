namespace Api.Agente.Domain.Abstractions;

public interface IFileStorage
{
    Task<StoredFileDescriptor> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
}

public sealed record StoredFileDescriptor(
    Guid FileId,
    string Sha256,
    string StoragePath,
    long SizeBytes);
