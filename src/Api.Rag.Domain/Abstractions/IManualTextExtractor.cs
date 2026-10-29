namespace Api.Rag.Domain.Abstractions;

public sealed record ExtractedManualText(
    string PlainText,
    string? DetectedFormat);

public interface IManualTextExtractor
{
    bool CanExtract(string fileName, string? contentType);

    Task<ExtractedManualText> ExtractAsync(
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}
