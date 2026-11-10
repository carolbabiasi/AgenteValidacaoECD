using Api.Agente.Application.Rag;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Agente.Tests.Unit;

public sealed class RagContextBuilderTests
{
    private const string UnknownManualLimitation = "MANUAL_VERSION_UNKNOWN";
    private const string KindMismatchLimitation = "MANUAL_VERSION_KIND_MISMATCH";

    [Fact]
    public async Task BuildAsync_PrefereVersaoComDocumentKindManual()
    {
        var builder = CreateBuilder(
            new RagManualVersionInfo("GUIA-2024", new DateOnly(2024, 1, 1), "Active", DocumentKind: "Guia"),
            new RagManualVersionInfo("ECD-Manual-2025", new DateOnly(2025, 3, 1), "Active", DocumentKind: "Manual"));

        var snapshot = await builder.BuildAsync(CreateExecution(), []);

        Assert.Equal("ECD-Manual-2025", snapshot.ManualVersionResolved);
        Assert.Empty(snapshot.Limitations);
    }

    [Fact]
    public async Task BuildAsync_SemDocumentKindManual_UsaPrimeiraVersaoAtiva()
    {
        var builder = CreateBuilder(
            new RagManualVersionInfo("ECD-Legacy-2023", new DateOnly(2023, 5, 1), "Active"));

        var snapshot = await builder.BuildAsync(CreateExecution(), []);

        Assert.Equal("ECD-Legacy-2023", snapshot.ManualVersionResolved);
        Assert.DoesNotContain(
            snapshot.Limitations,
            l => l.StartsWith(UnknownManualLimitation, StringComparison.Ordinal));
        Assert.Contains(
            snapshot.Limitations,
            l => l.StartsWith(KindMismatchLimitation, StringComparison.Ordinal)
                 && l.Contains("ECD-Legacy-2023", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuildAsync_SemNenhumaVersao_RegistraLimitacao()
    {
        var builder = CreateBuilder();

        var snapshot = await builder.BuildAsync(CreateExecution(), []);

        Assert.Null(snapshot.ManualVersionResolved);
        Assert.Contains(
            snapshot.Limitations,
            l => l.StartsWith(UnknownManualLimitation, StringComparison.Ordinal));
    }

    private static RagContextBuilder CreateBuilder(params RagManualVersionInfo[] versions) =>
        new(new StubRagMcpClient(versions), NullLogger<RagContextBuilder>.Instance);

    private static ValidationExecution CreateExecution() => new()
    {
        Id = Guid.NewGuid(),
        CorrelationId = "corr-rag-context",
        SubmittedByUserId = "user-rag-context",
        DetectedLayoutVersion = "LECD",
    };

    private sealed class StubRagMcpClient(IReadOnlyList<RagManualVersionInfo> versions) : IRagMcpClient
    {
        public Task<RagManualSearchResponse> SearchManualAsync(
            string query,
            string? layoutVersion,
            string? manualVersion,
            int topK,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new RagManualSearchResponse([], "hybrid"));

        public Task<RagManualSearchHit?> GetChunkAsync(
            Guid chunkId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RagManualSearchHit?>(null);

        public Task<IReadOnlyList<RagManualVersionInfo>> ListManualVersionsAsync(
            string? layoutFamily,
            bool activeOnly,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(versions);
    }
}
