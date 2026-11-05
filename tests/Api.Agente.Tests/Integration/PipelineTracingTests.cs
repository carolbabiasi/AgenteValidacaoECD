using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Api.Agente.Application.Pipeline;
using Api.Agente.Tests.Fixtures;
using Api.Agente.Tests.Support;

namespace Api.Agente.Tests.Integration;

[Collection(AgenteTestsCollection.Name)]
public sealed class PipelineTracingTests
{
    private static readonly string[] ExpectedPhases =
    [
        "LayoutDetection",
        "StructuralValidation",
        "Consolidation",
        "RagRetrieval",
        "AgentAnalysis",
        "Persistence",
    ];

    private readonly AgenteWebApplicationFactory _factory;

    public PipelineTracingTests(AgenteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Pipeline_EmitsRunAndPhaseSpans_CorrelatedByExecutionId()
    {
        var stopped = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == EcdPipelineTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add,
        };
        ActivitySource.AddActivityListener(listener);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DevJwtTokenFactory.CreateValidatorToken("tracing-user"));

        var executionId = await SubmitAndWaitAsync(client);
        var executionTag = executionId.ToString();

        Activity? run = null;
        for (var i = 0; i < 20 && run is null; i++)
        {
            run = stopped.FirstOrDefault(a =>
                a.OperationName == EcdPipelineTelemetry.RunActivityName &&
                (string?)a.GetTagItem(EcdPipelineTelemetry.ExecutionIdTag) == executionTag);
            if (run is null)
            {
                await Task.Delay(250);
            }
        }

        Assert.NotNull(run);
        Assert.Equal("Completed", run.GetTagItem("ecd.status"));

        var phases = stopped
            .Where(a => a.ParentSpanId == run.SpanId)
            .OrderBy(a => a.StartTimeUtc)
            .Select(a => (string?)a.GetTagItem(EcdPipelineTelemetry.PhaseTag))
            .ToArray();

        Assert.Equal(ExpectedPhases, phases);
    }

    private static async Task<Guid> SubmitAndWaitAsync(HttpClient client)
    {
        var content = SampleEcdFiles.ValidMinimal + $"\n|correlation|{Guid.NewGuid():N}|\n";
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "file", $"ecd-{Guid.NewGuid():N}.txt");

        var submit = await client.PostAsync("/api/v1/validations", form);
        submit.EnsureSuccessStatusCode();
        var body = await submit.Content.ReadFromJsonAsync<JsonElement>();
        var executionId = body.GetProperty("executionId").GetGuid();

        for (var i = 0; i < 40; i++)
        {
            var status = await client.GetFromJsonAsync<JsonElement>($"/api/v1/validations/{executionId}");
            if (status.GetProperty("status").GetString() is "Completed" or "Failed")
            {
                return executionId;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException("Validação não concluiu a tempo.");
    }
}
