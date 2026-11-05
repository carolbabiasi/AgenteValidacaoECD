using System.Text;
using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Enums;
using Api.Rag.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Tests.Integration;

[Collection(RagTestsCollection.Name)]
public sealed class DocumentKindActivationTests(RagWebApplicationFactory factory)
{
    [Fact]
    public async Task ActivateOnSuccess_OnlyArchivesSameDocumentCode()
    {
        if (!await RagDatabaseGate.TryMigrateAsync(factory.Services))
        {
            return;
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var submission = scope.ServiceProvider.GetRequiredService<IIngestionSubmissionService>();
        var pipeline = scope.ServiceProvider.GetRequiredService<IManualIngestionPipeline>();
        var retrieval = scope.ServiceProvider.GetRequiredService<IRagRetrievalService>();

        var suffix = Guid.NewGuid().ToString("N")[..8];

        async Task IngestAsync(DocumentKind kind, string code, string version, string content)
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var submitted = await submission.SubmitAsync(
                stream,
                new IngestionSubmissionRequest(
                    $"{code}.txt",
                    "text/plain",
                    version,
                    null,
                    "ECD",
                    ActivateOnSuccess: true,
                    DocumentKind: kind,
                    DocumentCode: code));
            await pipeline.RunAsync(submitted.IngestionJobId);
        }

        await IngestAsync(DocumentKind.Manual, "MANUAL", $"MAN-{suffix}", "Registro 0000\nManual base.");
        await IngestAsync(DocumentKind.Faq, "FAQ-RFB", $"FAQ-{suffix}", "## Prazo\nEntrega em junho.");

        var active = await retrieval.ListManualVersionsAsync("ECD", activeOnly: true);
        Assert.Contains(active, v => v.DocumentCode == "MANUAL" && v.Status == "Active");
        Assert.Contains(active, v => v.DocumentCode == "FAQ-RFB" && v.Status == "Active");
    }
}
