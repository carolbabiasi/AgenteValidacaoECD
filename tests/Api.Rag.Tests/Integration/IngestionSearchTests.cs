using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Api.Rag.Domain.Abstractions;
using Api.Rag.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Tests.Integration;

public sealed class IngestionSearchTests(RagWebApplicationFactory factory) : IClassFixture<RagWebApplicationFactory>
{
    [Fact]
    public async Task IngestionPipeline_ThenSearch_FindsRegistroChunk()
    {
        if (!await RagDatabaseGate.TryMigrateAsync(factory.Services))
        {
            return;
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var submission = scope.ServiceProvider.GetRequiredService<IIngestionSubmissionService>();
        var pipeline = scope.ServiceProvider.GetRequiredService<IManualIngestionPipeline>();
        var retrieval = scope.ServiceProvider.GetRequiredService<IRagRetrievalService>();

        const string manualText = """
            Registro I050 p. 142
            O registro I050 deve conter o plano de contas referencial da ECD.
            """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(manualText));
        var submitted = await submission.SubmitAsync(
            stream,
            "manual-ecd.txt",
            "text/plain",
            manualVersion: $"TEST-{Guid.NewGuid():N}",
            publicationDate: new DateOnly(2025, 3, 1),
            layoutFamily: "ECD",
            activateOnSuccess: true);

        await pipeline.RunAsync(submitted.IngestionJobId);

        var response = await retrieval.SearchManualAsync(
            new ManualSearchRequest(
                "plano de contas I050",
                LayoutVersion: "ECD",
                ManualVersion: null,
                TopK: 5,
                MinScore: 0.0));

        Assert.Equal("hybrid", response.RetrievalMode);
        Assert.Contains(
            response.Results,
            r => r.HierarchyPath.Contains("I050", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostIngestion_ReturnsAccepted()
    {
        if (!await RagDatabaseGate.TryMigrateAsync(factory.Services))
        {
            return;
        }

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", RagDevJwtTokenFactory.CreateRagAdminToken());

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("ECD"), "layoutFamily");
        form.Add(new StringContent($"REST-{Guid.NewGuid():N}"), "manualVersion");
        form.Add(new StringContent("true"), "activateOnSuccess");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("Registro I050\nConteúdo mínimo.")), "file", "manual.txt");

        var post = await client.PostAsync("/api/v1/ingestions", form);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, post.StatusCode);

        var body = await post.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var jobId = body.GetProperty("ingestionJobId").GetGuid();

        await using var scope = factory.Services.CreateAsyncScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<IManualIngestionPipeline>();
        await pipeline.RunAsync(jobId);

        var statusResponse = await client.GetAsync($"/api/v1/ingestions/{jobId}");
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Completed", status.GetProperty("status").GetString());
    }
}
