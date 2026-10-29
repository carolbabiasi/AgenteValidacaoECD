using System.Net.Http.Headers;
using System.Text.Json;
using Api.Agente.Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Api.Agente.Infrastructure.Mcp;

public sealed class RagMcpClient(
    IHttpClientFactory httpClientFactory,
    RagServiceJwtTokenFactory tokenFactory,
    IOptions<RagMcpOptions> options,
    ILogger<RagMcpClient> logger) : IRagMcpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<RagManualSearchResponse> SearchManualAsync(
        string query,
        string? layoutVersion,
        string? manualVersion,
        int topK,
        CancellationToken cancellationToken = default)
    {
        await using var client = await ConnectAsync(cancellationToken);
        var result = await client.CallToolAsync(
            "rag.search_manual",
            new Dictionary<string, object?>
            {
                ["query"] = query,
                ["layoutVersion"] = layoutVersion,
                ["manualVersion"] = manualVersion,
                ["topK"] = topK,
                ["minScore"] = 0.0,
            },
            cancellationToken: cancellationToken);

        var json = ExtractJson(result);
        var payload = JsonSerializer.Deserialize<SearchManualPayload>(json, JsonOptions)
            ?? throw new InvalidOperationException("Resposta MCP search_manual inválida.");

        var hits = payload.Results.Select(r => new RagManualSearchHit(
            r.ChunkId,
            r.ManualVersion,
            r.HierarchyPath,
            r.SourceLocation,
            r.Excerpt,
            r.Score)).ToList();

        return new RagManualSearchResponse(hits, payload.RetrievalMode ?? "hybrid");
    }

    public async Task<RagManualSearchHit?> GetChunkAsync(
        Guid chunkId,
        CancellationToken cancellationToken = default)
    {
        await using var client = await ConnectAsync(cancellationToken);
        var result = await client.CallToolAsync(
            "rag.get_chunk",
            new Dictionary<string, object?> { ["chunkId"] = chunkId },
            cancellationToken: cancellationToken);

        var json = ExtractJson(result);
        var payload = JsonSerializer.Deserialize<GetChunkPayload>(json, JsonOptions);
        if (payload is null)
        {
            return null;
        }

        return new RagManualSearchHit(
            payload.ChunkId,
            payload.ManualVersion,
            payload.HierarchyPath,
            payload.SourceLocation,
            payload.Content,
            1.0);
    }

    public async Task<IReadOnlyList<RagManualVersionInfo>> ListManualVersionsAsync(
        string? layoutFamily,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        await using var client = await ConnectAsync(cancellationToken);
        var result = await client.CallToolAsync(
            "rag.list_manual_versions",
            new Dictionary<string, object?>
            {
                ["layoutFamily"] = layoutFamily,
                ["activeOnly"] = activeOnly,
            },
            cancellationToken: cancellationToken);

        var json = ExtractJson(result);
        var payload = JsonSerializer.Deserialize<ListVersionsPayload>(json, JsonOptions)
            ?? throw new InvalidOperationException("Resposta MCP list_manual_versions inválida.");

        return payload.Items.Select(i =>
        {
            DateOnly? pub = null;
            if (!string.IsNullOrWhiteSpace(i.PublicationDate) &&
                DateOnly.TryParse(i.PublicationDate, out var parsed))
            {
                pub = parsed;
            }

            return new RagManualVersionInfo(i.ManualVersion, pub, i.Status);
        }).ToList();
    }

    private async Task<McpClient> ConnectAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var endpoint = new Uri(settings.BaseUrl, settings.McpPath);
        var httpClient = httpClientFactory.CreateClient(nameof(RagMcpClient));
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenFactory.CreateReaderToken());

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = endpoint },
            httpClient,
            loggerFactory: null);

        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    private static string ExtractJson(ModelContextProtocol.Protocol.CallToolResult result)
    {
        var text = result.Content?
            .OfType<ModelContextProtocol.Protocol.TextContentBlock>()
            .Select(c => c.Text)
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("MCP tool não retornou conteúdo textual.");
        }

        return text;
    }

    private sealed class SearchManualPayload
    {
        public List<SearchHitPayload> Results { get; set; } = [];

        public string? RetrievalMode { get; set; }
    }

    private sealed class SearchHitPayload
    {
        public Guid ChunkId { get; set; }

        public required string ManualVersion { get; set; }

        public required string HierarchyPath { get; set; }

        public string? SourceLocation { get; set; }

        public required string Excerpt { get; set; }

        public double Score { get; set; }
    }

    private sealed class GetChunkPayload
    {
        public Guid ChunkId { get; set; }

        public required string ManualVersion { get; set; }

        public required string HierarchyPath { get; set; }

        public string? SourceLocation { get; set; }

        public required string Content { get; set; }
    }

    private sealed class ListVersionsPayload
    {
        public List<VersionItemPayload> Items { get; set; } = [];
    }

    private sealed class VersionItemPayload
    {
        public required string ManualVersion { get; set; }

        public string? PublicationDate { get; set; }

        public required string Status { get; set; }
    }
}
