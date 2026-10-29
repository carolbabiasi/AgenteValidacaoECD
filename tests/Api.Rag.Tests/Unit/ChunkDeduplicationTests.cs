using Api.Rag.Application.Chunking;

namespace Api.Rag.Tests.Unit;

public sealed class ChunkDeduplicationTests
{
    [Fact]
    public void BuildChunkKey_IsStableForSameHierarchyAndHashPrefix()
    {
        const string content = "Texto do manual ECD";
        var hash = ChunkDeduplication.ComputeContentHash(content);
        var key1 = ChunkDeduplication.BuildChunkKey("Registro I050", hash);
        var key2 = ChunkDeduplication.BuildChunkKey("Registro I050", hash);

        Assert.Equal(key1, key2);
        Assert.Contains("Registro I050", key1, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldSkipChunk_WhenChunkKeyAlreadyExists()
    {
        var hash = ChunkDeduplication.ComputeContentHash("conteúdo");
        var key = ChunkDeduplication.BuildChunkKey("Capítulo 3", hash);
        var existingKeys = new HashSet<string>(StringComparer.Ordinal) { key };

        var skip = ChunkDeduplication.ShouldSkipChunk(
            key,
            hash,
            existingKeys,
            new Dictionary<string, string>());

        Assert.True(skip);
    }

    [Fact]
    public void StructuralChunker_SplitsByRegistroHeading()
    {
        const string text = """
            ## Introdução
            Texto introdutório.

            Registro I050 p. 142
            O registro I050 deve conter o plano de contas.
            """;

        var chunks = StructuralChunker.Chunk(text);

        Assert.Contains(chunks, c => c.HierarchyPath.Contains("I050", StringComparison.Ordinal));
        Assert.Contains(chunks, c => c.Content.Contains("plano de contas", StringComparison.Ordinal));
    }
}
