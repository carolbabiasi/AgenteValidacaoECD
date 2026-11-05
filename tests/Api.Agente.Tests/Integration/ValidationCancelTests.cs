using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Api.Agente.Infrastructure.Persistence;
using Api.Agente.Tests.Fixtures;
using Api.Agente.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Agente.Tests.Integration;

[Collection(AgenteTestsCollection.Name)]
public sealed class ValidationCancelTests
{
    private readonly AgenteWebApplicationFactory _factory;

    public ValidationCancelTests(AgenteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_QueuedExecution_CancelsExecutionAndJob()
    {
        const string userId = "cancel-owner";
        var executionId = await SeedExecutionAsync(userId, ValidationStatus.Queued, Guid.NewGuid().ToString("N"));
        using var client = CreateClient(userId);

        var response = await client.DeleteAsync($"/api/v1/validations/{executionId}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Queued", body.GetProperty("previousStatus").GetString());
        Assert.Equal("Cancelled", body.GetProperty("status").GetString());

        var status = await client.GetFromJsonAsync<JsonElement>($"/api/v1/validations/{executionId}");
        Assert.Equal("Cancelled", status.GetProperty("status").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperationalDbContext>();
        var job = await db.ValidationJobs.AsNoTracking().SingleAsync(j => j.ExecutionId == executionId);
        Assert.Equal(ValidationJobState.Cancelled, job.State);
    }

    [Fact]
    public async Task Delete_CompletedExecution_ReturnsConflict()
    {
        const string userId = "cancel-completed";
        var executionId = await SeedExecutionAsync(userId, ValidationStatus.Completed, Guid.NewGuid().ToString("N"));
        using var client = CreateClient(userId);

        var response = await client.DeleteAsync($"/api/v1/validations/{executionId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("NOT_CANCELLABLE", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_OtherUserExecution_ReturnsForbidden()
    {
        var executionId = await SeedExecutionAsync("cancel-real-owner", ValidationStatus.Queued, Guid.NewGuid().ToString("N"));
        using var intruder = CreateClient("cancel-intruder");

        var response = await intruder.DeleteAsync($"/api/v1/validations/{executionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownExecution_ReturnsNotFound()
    {
        using var client = CreateClient("cancel-unknown");

        var response = await client.DeleteAsync($"/api/v1/validations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_SameHashWithinWindow_ReturnsDuplicateSubmission()
    {
        const string userId = "duplicate-owner";
        var content = SampleEcdFiles.ValidMinimal + $"\n|correlation|{Guid.NewGuid():N}|\n";
        await SeedExecutionAsync(userId, ValidationStatus.Completed, ComputeSha256(content));
        using var client = CreateClient(userId);

        var response = await PostFileAsync(client, content);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("DUPLICATE_SUBMISSION", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_SameHashAfterCancellation_IsAccepted()
    {
        const string userId = "duplicate-after-cancel";
        var content = SampleEcdFiles.ValidMinimal + $"\n|correlation|{Guid.NewGuid():N}|\n";
        await SeedExecutionAsync(userId, ValidationStatus.Cancelled, ComputeSha256(content));
        using var client = CreateClient(userId);

        var response = await PostFileAsync(client, content);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private async Task<Guid> SeedExecutionAsync(string userId, ValidationStatus status, string sha256)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperationalDbContext>();

        var file = new StoredFile
        {
            Id = Guid.NewGuid(),
            Sha256 = sha256,
            OriginalFileName = "seed.txt",
            ContentType = "text/plain",
            SizeBytes = 1,
            StoragePath = "seed/does-not-exist.txt",
            UploadedAt = DateTimeOffset.UtcNow,
        };

        var execution = new ValidationExecution
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString("N"),
            Status = status,
            SubmittedByUserId = userId,
            OriginalFileId = file.Id,
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        var job = new ValidationJob
        {
            Id = Guid.NewGuid(),
            ExecutionId = execution.Id,
            Attempt = 1,
            State = status == ValidationStatus.Queued ? ValidationJobState.Pending : ValidationJobState.Done,
            // Mantém o job fora do alcance do ValidationJobProcessor durante o teste.
            LockedUntil = DateTimeOffset.UtcNow.AddHours(1),
        };

        db.StoredFiles.Add(file);
        db.ValidationExecutions.Add(execution);
        db.ValidationJobs.Add(job);
        await db.SaveChangesAsync();
        return execution.Id;
    }

    private static Task<HttpResponseMessage> PostFileAsync(HttpClient client, string content)
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", $"ecd-{Guid.NewGuid():N}.txt");
        return client.PostAsync("/api/v1/validations", form);
    }

    private static string ComputeSha256(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private HttpClient CreateClient(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DevJwtTokenFactory.CreateValidatorToken(userId));
        return client;
    }
}
