using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Api.Rag.Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Api.Rag.Infrastructure.Embeddings;

public sealed class OpenAiEmbeddingService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAiEmbeddingService> logger) : IEmbeddingService
{
    public int Dimensions => 1536;

    public Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken = default) =>
        EmbedSingleAsync(text, cancellationToken);

    public async Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>(texts.Count);
        foreach (var text in texts)
        {
            results.Add(await EmbedSingleAsync(text, cancellationToken));
        }

        return results;
    }

    private async Task<float[]> EmbedSingleAsync(string text, CancellationToken cancellationToken)
    {
        var model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        var request = new EmbeddingRequest(model, text);

        using var response = await httpClient.PostAsJsonAsync("embeddings", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("OpenAI embeddings falhou: {Status} {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Resposta OpenAI inválida.");

        var vector = payload.Data.FirstOrDefault()?.Embedding
            ?? throw new InvalidOperationException("Embedding ausente na resposta OpenAI.");

        if (vector.Length != Dimensions)
        {
            throw new InvalidOperationException($"Dimensão {vector.Length} incompatível com PgVector(1536).");
        }

        return vector;
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] List<EmbeddingData> Data);

    private sealed record EmbeddingData(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
