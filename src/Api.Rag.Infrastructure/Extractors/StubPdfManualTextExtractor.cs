using Api.Rag.Domain.Abstractions;

namespace Api.Rag.Infrastructure.Extractors;

public sealed class StubPdfManualTextExtractor : IManualTextExtractor
{
    public bool CanExtract(string fileName, string? contentType)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ExtractedManualText> ExtractAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "PDF_STUB: extração PDF não habilitada nesta versão; envie .txt/.md para homologação.");
    }
}
