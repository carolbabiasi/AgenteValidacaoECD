using System.Security.Cryptography;
using System.Text;
using Api.Rag.Domain.Abstractions;

namespace Api.Rag.Infrastructure.Embeddings;

public sealed class DeterministicEmbeddingService : IEmbeddingService
{
    public int Dimensions => 1536;

    public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(Embed(text));

    public Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<float[]>>(texts.Select(Embed).ToList());

    private static float[] Embed(string text)
    {
        var vector = new float[1536];
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (hash[i % hash.Length] / 255f) * 2f - 1f;
        }

        Normalize(vector);
        return vector;
    }

    private static void Normalize(Span<float> vector)
    {
        double sum = 0;
        foreach (var value in vector)
        {
            sum += value * value;
        }

        var norm = Math.Sqrt(sum);
        if (norm <= 0)
        {
            return;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / norm);
        }
    }
}
