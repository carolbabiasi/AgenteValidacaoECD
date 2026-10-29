using Api.Rag.Domain.Abstractions;

namespace Api.Rag.Infrastructure.Extractors;

public sealed class CompositeManualTextExtractor(IEnumerable<IManualTextExtractor> extractors) : IManualTextExtractor
{
    public bool CanExtract(string fileName, string? contentType) =>
        extractors.Any(e => e.CanExtract(fileName, contentType));

    public Task<ExtractedManualText> ExtractAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var extractor = extractors.FirstOrDefault(e => e.CanExtract(fileName, contentType))
            ?? throw new NotSupportedException($"Formato não suportado: {fileName}");

        return extractor.ExtractAsync(content, fileName, contentType, cancellationToken);
    }
}
