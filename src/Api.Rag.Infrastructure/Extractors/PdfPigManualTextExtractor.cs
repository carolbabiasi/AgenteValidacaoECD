using System.Text;
using System.Text.RegularExpressions;
using Api.Rag.Application.Chunking;
using Api.Rag.Domain.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Api.Rag.Infrastructure.Extractors;

public sealed partial class PdfPigManualTextExtractor : IManualTextExtractor
{
    private const int EdgeLinesPerPage = 4;

    public bool CanExtract(string fileName, string? contentType)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ExtractedManualText> ExtractAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var pages = new List<(int Number, string[] Lines)>();
        using (var document = PdfDocument.Open(buffer.ToArray()))
        {
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = ContentOrderTextExtractor.GetText(page);
                pages.Add((page.Number, text.Replace("\r\n", "\n").Split('\n')));
            }
        }

        var repeatedEdges = FindRepeatedEdgeLines(pages.Select(p => p.Lines).ToList());

        var output = new StringBuilder();
        foreach (var (number, lines) in pages)
        {
            output.AppendLine(StructuralChunker.FormatPageMarker(number));
            foreach (var line in lines)
            {
                if (!repeatedEdges.Contains(Normalize(line)))
                {
                    output.AppendLine(line.TrimEnd());
                }
            }
        }

        return new ExtractedManualText(output.ToString(), ".pdf");
    }

    private static HashSet<string> FindRepeatedEdgeLines(IReadOnlyList<string[]> pages)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var lines in pages)
        {
            var nonEmpty = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var edges = nonEmpty.Take(EdgeLinesPerPage)
                .Concat(nonEmpty.TakeLast(EdgeLinesPerPage))
                .Select(Normalize)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var edge in edges)
            {
                counts[edge] = counts.GetValueOrDefault(edge) + 1;
            }
        }

        var threshold = Math.Max(2, pages.Count / 2 + 1);
        return counts.Where(kv => kv.Value >= threshold).Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);
    }

    private static string Normalize(string line) => Digits().Replace(line.Trim(), "#");

    [GeneratedRegex(@"\d+")]
    private static partial Regex Digits();
}
