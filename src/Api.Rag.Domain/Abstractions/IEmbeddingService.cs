namespace Api.Rag.Domain.Abstractions;

public interface IEmbeddingService
{
    int Dimensions { get; }

    Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
