using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Enums;
using Api.Rag.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Tests.Integration;

[Collection(RagTestsCollection.Name)]
public sealed class OfficialCorpusSearchTests(RagWebApplicationFactory factory)
{
    [Fact]
    public async Task ActiveCorpus_ReturnsManualFaqAndNormHitsWithProvenance()
    {
        if (!await RagDatabaseGate.TryMigrateAsync(factory.Services))
        {
            return;
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var retrieval = scope.ServiceProvider.GetRequiredService<IRagRetrievalService>();

        var active = await retrieval.ListManualVersionsAsync("ECD", activeOnly: true);
        if (!active.Any(v => v.ManualVersion == "ECD-L9-2026-05"))
        {
            return;
        }

        var manual = await retrieval.SearchManualAsync(
            new ManualSearchRequest(
                "registro I050 plano de contas referencial",
                "ECD",
                null,
                TopK: 5,
                DocumentKind: DocumentKind.Manual));
        Assert.Contains(
            manual.Results,
            r => r.SourceLocation is not null &&
                 r.SourceLocation.Contains('p', StringComparison.OrdinalIgnoreCase));

        var faq = await retrieval.SearchManualAsync(
            new ManualSearchRequest(
                "ReceitanetBX arquivo original transmitida",
                "ECD",
                null,
                TopK: 5,
                DocumentKind: DocumentKind.Faq));
        Assert.NotEmpty(faq.Results);

        var norm = await retrieval.SearchManualAsync(
            new ManualSearchRequest(
                "Art. 5º último dia útil junho",
                "ECD",
                null,
                TopK: 3,
                DocumentKind: DocumentKind.Norm));
        Assert.Contains(norm.Results, r => r.DocumentCode == "IN-RFB-2003-2021");
    }
}
