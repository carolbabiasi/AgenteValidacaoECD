using Api.Rag.Application.Chunking;

namespace Api.Rag.Tests.Unit;

public sealed class StructuralChunkerTests
{
    [Fact]
    public void Chunk_UsesPageMarkersAsSourceLocation()
    {
        var text = string.Join('\n',
            StructuralChunker.FormatPageMarker(64),
            "REGISTRO 0000: ABERTURA DO ARQUIVO DIGITAL",
            "Texto da página 64.",
            StructuralChunker.FormatPageMarker(65),
            "Continuação na página 65.");

        var chunk = Assert.Single(StructuralChunker.Chunk(text));

        Assert.Equal("REGISTRO 0000: ABERTURA DO ARQUIVO DIGITAL", chunk.HierarchyPath);
        Assert.Equal("p. 64–65", chunk.SourceLocation);
        Assert.DoesNotContain("[[p.", chunk.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Chunk_IgnoresTableOfContentsEntriesAsHeadings()
    {
        const string text = """
            Registro 0000: Abertura do Arquivo Digital ................................ 64
            Registro 0001: Abertura do Bloco 0 ........................................ 75
            Capítulo 3 – Blocos e Registros
            REGISTRO 0001: ABERTURA DO BLOCO 0
            Conteúdo do registro 0001.
            """;

        var chunks = StructuralChunker.Chunk(text);

        Assert.DoesNotContain(chunks, c => c.HierarchyPath.Contains("......", StringComparison.Ordinal));
        Assert.Contains(chunks, c =>
            c.HierarchyPath == "Capítulo 3 – Blocos e Registros > REGISTRO 0001: ABERTURA DO BLOCO 0" &&
            c.Content == "Conteúdo do registro 0001.");
    }

    [Fact]
    public void Chunk_NumberedSectionOnlyCountsWhenPrefixMatchesChapter()
    {
        const string text = """
            Capítulo 1 – Informações Gerais
            1.13. Assinatura do Livro Digital
            Regras de assinatura:
            4.1. Um e-CNPJ que coincida com o CNPJ do declarante
            4.2. Um e-CPF
            """;

        var chunk = Assert.Single(StructuralChunker.Chunk(text));

        Assert.Equal("Capítulo 1 – Informações Gerais > 1.13. Assinatura do Livro Digital", chunk.HierarchyPath);
        Assert.Contains("4.1. Um e-CNPJ", chunk.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Chunk_SentenceStartingWithRegistroIsNotHeading()
    {
        const string text = """
            REGISTRO J150: DEMONSTRAÇÃO DO RESULTADO
            Regra aplicada quando o
            registro J150 – J150.NU_ORDEM (Campo 02) – são diferentes.
            Registro que identifica a demonstração.
            """;

        var chunk = Assert.Single(StructuralChunker.Chunk(text));

        Assert.Equal("REGISTRO J150: DEMONSTRAÇÃO DO RESULTADO", chunk.HierarchyPath);
    }

    [Fact]
    public void Chunk_LongSection_SplitsOnLineBoundariesWithinLimit()
    {
        var lines = Enumerable.Range(1, 120).Select(i => $"Linha {i:D3} " + new string('x', 40));
        var text = "## Seção longa\n" + string.Join('\n', lines);

        var chunks = StructuralChunker.Chunk(text);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Content.Length <= StructuralChunker.MaxChunkCharacters));
        Assert.All(chunks, c => Assert.StartsWith("Linha ", c.Content, StringComparison.Ordinal));
        Assert.Equal("Seção longa (parte 1)", chunks[0].HierarchyPath);
    }

    [Fact]
    public void BuildChunkKey_LongPath_StaysWithinColumnLimitAndIsDeterministic()
    {
        var path = "Capítulo 3 – Blocos e Registros da ECD – Leiaute 9 – A partir do Ano-Calendário 2020 > " +
                   "REGISTRO 0000: ABERTURA DO ARQUIVO DIGITAL E IDENTIFICAÇÃO DO EMPRESÁRIO OU DA SOCIEDADE (parte 1)";
        var hash = ChunkDeduplication.ComputeContentHash("conteúdo");

        var key = ChunkDeduplication.BuildChunkKey(path, hash);
        var otherPart = ChunkDeduplication.BuildChunkKey(path.Replace("(parte 1)", "(parte 2)"), hash);

        Assert.True(key.Length <= ChunkDeduplication.MaxChunkKeyLength);
        Assert.Equal(key, ChunkDeduplication.BuildChunkKey(path, hash));
        Assert.NotEqual(key, otherPart);
    }
}
