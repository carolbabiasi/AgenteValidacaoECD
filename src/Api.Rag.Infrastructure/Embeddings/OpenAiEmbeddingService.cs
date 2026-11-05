using System.Net;
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
    private const int MaxAttempts = 4;

    public int Dimensions => 1536;

    public async Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken = default) =>
        (await EmbedBatchAsync([text], cancellationToken))[0];

    public Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default) =>
        texts.Count == 0
            ? Task.FromResult<IReadOnlyList<float[]>>([])
            : EmbedBatchAsync(texts, cancellationToken);

    private async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        var model = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        var request = new EmbeddingRequest(model, texts);

        for (var attempt = 1; ; attempt++)
        {
            using var response = await httpClient.PostAsJsonAsync("embeddings", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken)
                    ?? throw new InvalidOperationException("Resposta OpenAI inválida.");
                return ToVectors(payload, texts.Count);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var transient = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (!transient || attempt >= MaxAttempts)
            {
                logger.LogError("OpenAI embeddings falhou: {Status} {Body}", response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
            logger.LogWarning(
                "OpenAI embeddings {Status}; tentativa {Attempt}/{MaxAttempts}, nova tentativa em {Delay}",
                response.StatusCode,
                attempt,
                MaxAttempts,
                delay);
            await Task.Delay(delay, cancellationToken);
        }
    }

    private IReadOnlyList<float[]> ToVectors(EmbeddingResponse payload, int expected)
    {
        if (payload.Data.Count != expected)
        {
            throw new InvalidOperationException(
                $"OpenAI retornou {payload.Data.Count} embeddings; esperado {expected}.");
        }

        var vectors = payload.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToList();
        foreach (var vector in vectors)
        {
            if (vector.Length != Dimensions)
            {
                throw new InvalidOperationException($"Dimensão {vector.Length} incompatível com PgVector(1536).");
            }
        }

        return vectors;
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] List<EmbeddingData> Data);

    private sealed record EmbeddingData(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
