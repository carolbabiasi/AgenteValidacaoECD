using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Rag.Domain.Abstractions;
using Api.Rag.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Api.Rag.Tests.Integration;

[Collection(RagTestsCollection.Name)]
public sealed class McpContractTests(RagWebApplicationFactory factory)
{
    [Theory]
    [InlineData("rag.search_manual", new[] { "query", "layoutVersion", "manualVersion", "documentKind", "topK", "minScore" })]
    [InlineData("rag.get_chunk", new[] { "chunkId" })]
    [InlineData("rag.list_manual_versions", new[] { "layoutFamily", "activeOnly" })]
    public async Task ListTools_ExposesFlatArgumentsFromContract(string toolName, string[] expectedArguments)
    {
        await using var client = await ConnectAsync();

        var tools = await client.ListToolsAsync();
        var tool = Assert.Single(tools, t => t.Name == toolName);

        var properties = tool.JsonSchema.GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .ToArray();

        Assert.Equal(expectedArguments.Order(), properties.Order());
    }

    [Fact]
    public async Task CallTools_WithContractArguments_ReturnsActiveManualAndHits()
    {
        if (!await RagDatabaseGate.TryMigrateAsync(factory.Services))
        {
            return;
        }

        var manualVersion = $"MCP-{Guid.NewGuid().ToString("N")[..12]}";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var submission = scope.ServiceProvider.GetRequiredService<IIngestionSubmissionService>();
            var pipeline = scope.ServiceProvider.GetRequiredService<IManualIngestionPipeline>();

            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Registro I050\nPlano de contas referencial."));
            var submitted = await submission.SubmitAsync(
                stream,
                new IngestionSubmissionRequest(
                    "manual-mcp.txt",
                    "text/plain",
                    manualVersion,
                    new DateOnly(2025, 3, 1),
                    "ECD",
                    ActivateOnSuccess: true));
            await pipeline.RunAsync(submitted.IngestionJobId);
        }

        await using var client = await ConnectAsync();

        var versions = await client.CallToolAsync(
            "rag.list_manual_versions",
            new Dictionary<string, object?> { ["layoutFamily"] = "ECD", ["activeOnly"] = true });
        Assert.NotEqual(true, versions.IsError);
        Assert.Contains(manualVersion, ReadText(versions), StringComparison.Ordinal);

        var search = await client.CallToolAsync(
            "rag.search_manual",
            new Dictionary<string, object?>
            {
                ["query"] = "plano de contas I050",
                ["layoutVersion"] = "ECD",
                ["manualVersion"] = manualVersion,
                ["topK"] = 3,
                ["minScore"] = 0.0,
            });
        Assert.NotEqual(true, search.IsError);

        using var payload = JsonDocument.Parse(ReadText(search));
        Assert.NotEmpty(payload.RootElement.GetProperty("results").EnumerateArray());
    }

    private async Task<McpClient> ConnectAsync()
    {
        var httpClient = factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", RagDevJwtTokenFactory.CreateRagAdminToken());

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(httpClient.BaseAddress!, "/mcp") },
            httpClient,
            loggerFactory: null,
            ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }

    private static string ReadText(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));
}
