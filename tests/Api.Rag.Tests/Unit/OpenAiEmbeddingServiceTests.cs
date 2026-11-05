using System.Net;
using System.Text;
using System.Text.Json;
using Api.Rag.Infrastructure.Embeddings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Rag.Tests.Unit;

public sealed class OpenAiEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedDocuments_SendsSingleBatchAndOrdersByIndex()
    {
        var handler = new StubHandler(_ => Ok(("b", 1), ("a", 0)));
        var service = CreateService(handler);

        var vectors = await service.EmbedDocumentsAsync(["texto a", "texto b"]);

        Assert.Equal(1, handler.Calls);
        using var request = JsonDocument.Parse(handler.Bodies[0]);
        Assert.Equal(2, request.RootElement.GetProperty("input").GetArrayLength());
        Assert.Equal(0f, vectors[0][0]);
        Assert.Equal(1f, vectors[1][0]);
    }

    [Fact]
    public async Task EmbedDocuments_RetriesTransientStatus()
    {
        var handler = new StubHandler(call => call == 1
            ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Headers = { RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero) },
            }
            : Ok(("a", 0)));
        var service = CreateService(handler);

        var vectors = await service.EmbedDocumentsAsync(["texto a"]);

        Assert.Equal(2, handler.Calls);
        Assert.Single(vectors);
    }

    [Fact]
    public async Task EmbedDocuments_DoesNotRetryClientError()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.EmbedDocumentsAsync(["texto a"]));
        Assert.Equal(1, handler.Calls);
    }

    private static OpenAiEmbeddingService CreateService(StubHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test/v1/") },
            new ConfigurationBuilder().Build(),
            NullLogger<OpenAiEmbeddingService>.Instance);

    private static HttpResponseMessage Ok(params (string Name, int Index)[] items)
    {
        var data = items.Select(i => new
        {
            index = i.Index,
            embedding = Enumerable.Repeat((float)i.Index, 1536).ToArray(),
        });
        var json = JsonSerializer.Serialize(new { data });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private sealed class StubHandler(Func<int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return respond(Calls);
        }
    }
}
