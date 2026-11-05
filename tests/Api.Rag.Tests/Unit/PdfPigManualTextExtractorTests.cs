using Api.Rag.Application.Chunking;
using Api.Rag.Infrastructure.Extractors;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Api.Rag.Tests.Unit;

public sealed class PdfPigManualTextExtractorTests
{
    [Fact]
    public async Task Extract_EmitsPageMarkersAndDropsRepeatedHeaders()
    {
        var pdf = BuildPdf(
            ["REGISTRO 0000: ABERTURA DO ARQUIVO", "Campo 01 - Tipo de Registro"],
            ["Continuacao do registro 0000"],
            ["REGISTRO 0001: ABERTURA DO BLOCO 0"]);

        await using var stream = new MemoryStream(pdf);
        var extracted = await new PdfPigManualTextExtractor().ExtractAsync(stream, "manual.pdf", "application/pdf");

        Assert.Contains(StructuralChunker.FormatPageMarker(1), extracted.PlainText, StringComparison.Ordinal);
        Assert.Contains(StructuralChunker.FormatPageMarker(3), extracted.PlainText, StringComparison.Ordinal);
        Assert.Contains("Continuacao do registro 0000", extracted.PlainText, StringComparison.Ordinal);
        Assert.DoesNotContain("Manual de Orientacao", extracted.PlainText, StringComparison.Ordinal);
        Assert.DoesNotContain("Pagina 2 de 3", extracted.PlainText, StringComparison.Ordinal);

        var chunks = StructuralChunker.Chunk(extracted.PlainText);
        Assert.Contains(chunks, c => c.HierarchyPath.StartsWith("REGISTRO 0000", StringComparison.Ordinal) &&
                                     c.SourceLocation == "p. 1–2");
    }

    [Fact]
    public void CanExtract_AcceptsPdfByExtensionOrContentType()
    {
        var extractor = new PdfPigManualTextExtractor();

        Assert.True(extractor.CanExtract("Manual.PDF", null));
        Assert.True(extractor.CanExtract("manual", "application/pdf"));
        Assert.False(extractor.CanExtract("manual.md", "text/markdown"));
    }

    private static byte[] BuildPdf(params string[][] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        for (var i = 0; i < pages.Length; i++)
        {
            var page = builder.AddPage(595, 842);
            page.AddText("Manual de Orientacao do Leiaute 9 da ECD", 9, new PdfPoint(40, 810), font);
            page.AddText($"Pagina {i + 1} de {pages.Length}", 9, new PdfPoint(40, 795), font);

            var y = 740;
            foreach (var line in pages[i])
            {
                page.AddText(line, 11, new PdfPoint(40, y), font);
                y -= 20;
            }
        }

        return builder.Build();
    }
}
