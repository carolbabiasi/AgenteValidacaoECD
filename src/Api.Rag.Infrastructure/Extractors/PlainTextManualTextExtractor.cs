using System.Text;
using Api.Rag.Domain.Abstractions;

namespace Api.Rag.Infrastructure.Extractors;

public sealed class PlainTextManualTextExtractor : IManualTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".html", ".htm",
    };

    public bool CanExtract(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (SupportedExtensions.Contains(extension))
        {
            return true;
        }

        return contentType is not null &&
               (contentType.Contains("text/", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("html", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ExtractedManualText> ExtractAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        return new ExtractedManualText(text, Path.GetExtension(fileName));
    }
}
