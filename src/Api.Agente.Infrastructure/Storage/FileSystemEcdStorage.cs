using System.Security.Cryptography;
using Api.Agente.Domain.Abstractions;
using Microsoft.Extensions.Options;

namespace Api.Agente.Infrastructure.Storage;

public sealed class FileSystemEcdStorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "storage/ecd-uploads";
}

public sealed class FileSystemEcdStorage(IOptions<FileSystemEcdStorageOptions> options) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(options.Value.RootPath);

    public async Task<StoredFileDescriptor> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_root);

        var fileId = Guid.NewGuid();
        var safeName = Path.GetFileName(originalFileName);
        var relativePath = Path.Combine(fileId.ToString("N"), safeName);
        var absolutePath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var fileStream = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        using var sha = SHA256.Create();
        await using var hashingStream = new CryptoStream(fileStream, sha, CryptoStreamMode.Write);

        await content.CopyToAsync(hashingStream, cancellationToken);
        await hashingStream.FlushFinalBlockAsync(cancellationToken);

        var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        var size = fileStream.Length;

        return new StoredFileDescriptor(fileId, hash, relativePath.Replace('\\', '/'), size);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!absolutePath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Caminho de armazenamento inválido.");
        }

        Stream stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }
}
