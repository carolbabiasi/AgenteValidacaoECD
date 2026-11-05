using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.Agente.Tests.Fixtures;
using Api.Agente.Tests.Support;

namespace AgenteValidacaoECD.IntegrationTests;

[Collection(AgenteIntegrationCollection.Name)]
public sealed class AgentExplanationTests(AgenteWebApplicationFactory factory)
{
    [Fact]
    public async Task CompletedValidation_ReportContainsRagSnapshotMetadata()
    {
        using var client = CreateClient();
        var executionId = await SubmitAsync(
            client,
            WithUniqueCorrelation(SampleEcdFiles.MissingOpening),
            $"missing-{Guid.NewGuid():N}.txt");
        await WaitForTerminalStatusAsync(client, executionId);

        var status = await client.GetFromJsonAsync<JsonElement>($"/api/v1/validations/{executionId}");
        Assert.Equal("Completed", status.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(status.GetProperty("manualVersionUsed").GetString()));

        var report = await client.GetFromJsonAsync<JsonElement>($"/api/v1/validations/{executionId}/report");
        var limitations = report.GetProperty("limitations").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(limitations, l => l is not null && l.Contains("homolog", StringComparison.OrdinalIgnoreCase));

        var findings = report.GetProperty("findings");
        if (findings.GetArrayLength() > 0)
        {
            var first = findings[0];
            Assert.True(first.TryGetProperty("ragReferences", out var refs));
            Assert.True(refs.GetArrayLength() >= 1);
        }
    }

    [Fact]
    public async Task ChatAfterCompletion_ReturnsDisclaimer()
    {
        using var client = CreateClient();
        var executionId = await SubmitAsync(
            client,
            WithUniqueCorrelation(SampleEcdFiles.ValidMinimal),
            $"valid-{Guid.NewGuid():N}.txt");
        await WaitForTerminalStatusAsync(client, executionId);

        var chatResponse = await client.PostAsJsonAsync(
            $"/api/v1/validations/{executionId}/chat",
            new { message = "Explique o achado principal" });

        chatResponse.EnsureSuccessStatusCode();
        var body = await chatResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("disclaimer").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("answer").GetProperty("text").GetString()));
    }

    private HttpClient CreateClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DevJwtTokenFactory.CreateValidatorToken());
        return client;
    }

    private static string WithUniqueCorrelation(string content) =>
        content + $"\n|correlation|{Guid.NewGuid():N}|\n";

    private static async Task<Guid> SubmitAsync(HttpClient client, string content, string fileName)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", fileName);

        var response = await client.PostAsync("/api/v1/validations", form);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("executionId").GetGuid();
    }

    private static async Task<JsonElement> WaitForTerminalStatusAsync(HttpClient client, Guid executionId)
    {
        for (var i = 0; i < 40; i++)
        {
            var status = await client.GetFromJsonAsync<JsonElement>($"/api/v1/validations/{executionId}");
            var value = status.GetProperty("status").GetString();
            if (value is "Completed" or "Failed")
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException("Validação não concluiu a tempo.");
    }
}
