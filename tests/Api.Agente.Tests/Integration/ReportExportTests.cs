using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Api.Agente.Tests.Fixtures;
using Api.Agente.Tests.Support;

namespace Api.Agente.Tests.Integration;

[Collection(AgenteTestsCollection.Name)]
public sealed class ReportExportTests
{
    private readonly AgenteWebApplicationFactory _factory;

    public ReportExportTests(AgenteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Export_OtherUser_ReturnsForbidden()
    {
        var ownerClient = CreateClient("owner-user");
        var executionId = await SubmitAndWaitAsync(ownerClient);

        using var intruderClient = CreateClient("intruder-user");
        var response = await intruderClient.GetAsync(
            $"/api/v1/validations/{executionId}/report/export?format=csv");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Export_Owner_ReturnsCsvWithFindings()
    {
        var client = CreateClient("export-owner");
        var executionId = await SubmitAndWaitAsync(
            client,
            SampleEcdFiles.MissingOpening + $"\n|correlation|{Guid.NewGuid():N}|\n");

        var response = await client.GetAsync(
            $"/api/v1/validations/{executionId}/report/export?format=csv");

        response.EnsureSuccessStatusCode();
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Severity", csv, StringComparison.Ordinal);
        Assert.Contains("High", csv, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListHistory_OwnerCannotReadPeerExecution()
    {
        var ownerA = CreateClient("history-user-a");
        var ownerB = CreateClient("history-user-b");

        var peerExecutionId = await SubmitAndWaitAsync(ownerB);

        var listA = await ownerA.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/v1/validations?pageSize=50");
        Assert.True(listA.GetProperty("totalCount").GetInt32() >= 0);

        var peerStatus = await ownerA.GetAsync($"/api/v1/validations/{peerExecutionId}");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, peerStatus.StatusCode);
    }

    private HttpClient CreateClient(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DevJwtTokenFactory.CreateValidatorToken(userId));
        return client;
    }

    private static async Task<Guid> SubmitAndWaitAsync(
        HttpClient client,
        string? content = null)
    {
        content ??= SampleEcdFiles.ValidMinimal + $"\n|correlation|{Guid.NewGuid():N}|\n";

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", $"ecd-{Guid.NewGuid():N}.txt");

        var submit = await client.PostAsync("/api/v1/validations", form);
        submit.EnsureSuccessStatusCode();
        var body = await submit.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var executionId = body.GetProperty("executionId").GetGuid();

        for (var i = 0; i < 40; i++)
        {
            var status = await client.GetFromJsonAsync<System.Text.Json.JsonElement>(
                $"/api/v1/validations/{executionId}");
            var value = status.GetProperty("status").GetString();
            if (value is "Completed" or "Failed")
            {
                return executionId;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException("Validação não concluiu a tempo.");
    }
}
