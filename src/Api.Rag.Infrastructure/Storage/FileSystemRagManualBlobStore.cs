using Api.Rag.Domain.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Api.Rag.Infrastructure.Storage;

public sealed class FileSystemRagManualBlobStore(IConfiguration configuration) : IRagManualBlobStore
{
    private string RootPath =>
        configuration["Rag:StorageRoot"] ?? Path.Combine(AppContext.BaseDirectory, "storage", "rag-manuals");

    public async Task<string> SaveAsync(
        Guid manualDocumentId,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(RootPath, manualDocumentId.ToString("N"));
        Directory.CreateDirectory(directory);
        var safeName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(directory, safeName);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);

        return fullPath;
    }

    public Task<Stream> OpenReadAsync(
        Guid manualDocumentId,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(RootPath, manualDocumentId.ToString("N"));
        if (!Directory.Exists(directory))
        {
            throw new FileNotFoundException("Manual não encontrado no storage.", directory);
        }

        var file = Directory.GetFiles(directory).FirstOrDefault()
            ?? throw new FileNotFoundException("Arquivo do manual ausente.", directory);

        Stream stream = File.OpenRead(file);
        return Task.FromResult(stream);
    }
}
