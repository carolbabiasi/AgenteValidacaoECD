using Api.Agente.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Api.Agente.Tests.Storage;

public sealed class FileSystemEcdStorageTests
{
    [Fact]
    public async Task SaveAsync_ComputesSha256AndImmutablePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "ecd-tests", Guid.NewGuid().ToString("N"));
        var storage = new FileSystemEcdStorage(Options.Create(new FileSystemEcdStorageOptions { RootPath = root }));

        await using var stream = new MemoryStream("ecd-linha-teste"u8.ToArray());
        var descriptor = await storage.SaveAsync(stream, "arquivo.txt", "text/plain");

        Assert.Equal(64, descriptor.Sha256.Length);
        Assert.True(descriptor.SizeBytes > 0);
        Assert.DoesNotContain("..", descriptor.StoragePath);

        await using (var read = await storage.OpenReadAsync(descriptor.StoragePath))
        using (var reader = new StreamReader(read))
        {
            var content = await reader.ReadToEndAsync();
            Assert.Equal("ecd-linha-teste", content);
        }

        Directory.Delete(root, recursive: true);
    }
}
