using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Api.Agente.Tests.Fixtures;
using Api.Agente.Tests.Support;

namespace AgenteValidacaoECD.IntegrationTests;

public sealed class ValidationPipelineTests(AgenteWebApplicationFactory factory) : IClassFixture<AgenteWebApplicationFactory>
{
    [Fact]
    public async Task HappyPath_CompletesWithValidatedSampleRules()
    {
        using var client = CreateClient();
        var executionId = await SubmitAsync(client, SampleEcdFiles.ValidMinimal, "valid.txt");
        var status = await WaitForTerminalStatusAsync(client, executionId);

        Assert.Equal("Completed", status.GetProperty("status").GetString());

        var reportResponse = await client.GetAsync($"/api/v1/validations/{executionId}/report");
        reportResponse.EnsureSuccessStatusCode();
        var report = await reportResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal("Completed", report.GetProperty("processingStatus").GetString());
    }

    [Fact]
    public async Task ViolationSample_ProducesFindings()
    {
        using var client = CreateClient();
        var executionId = await SubmitAsync(client, SampleEcdFiles.MissingOpening, "missing-0000.txt");
        await WaitForTerminalStatusAsync(client, executionId);

        var report = await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/validations/{executionId}/report");
        var findings = report.GetProperty("findings");
        Assert.True(findings.GetArrayLength() >= 1);
    }

    private HttpClient CreateClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DevJwtTokenFactory.CreateValidatorToken());
        return client;
    }

    private static async Task<Guid> SubmitAsync(HttpClient client, string content, string fileName)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", fileName);

        var response = await client.PostAsync("/api/v1/validations", form);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return body.GetProperty("executionId").GetGuid();
    }

    private static async Task<System.Text.Json.JsonElement> WaitForTerminalStatusAsync(HttpClient client, Guid executionId)
    {
        for (var i = 0; i < 30; i++)
        {
            var status = await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/validations/{executionId}");
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
