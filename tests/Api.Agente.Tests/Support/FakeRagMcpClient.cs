using Api.Agente.Domain.Abstractions;

namespace Api.Agente.Tests.Support;

public sealed class FakeRagMcpClient : IRagMcpClient
{
    public Task<RagManualSearchResponse> SearchManualAsync(
        string query,
        string? layoutVersion,
        string? manualVersion,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var hit = new RagManualSearchHit(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            manualVersion ?? "ECD-Manual-Mock",
            "Registro I050",
            "p. 142",
            "Trecho mock do manual para testes.",
            0.91);

        return Task.FromResult(new RagManualSearchResponse([hit], "hybrid"));
    }

    public Task<RagManualSearchHit?> GetChunkAsync(Guid chunkId, CancellationToken cancellationToken = default) =>
        Task.FromResult<RagManualSearchHit?>(null);

    public Task<IReadOnlyList<RagManualVersionInfo>> ListManualVersionsAsync(
        string? layoutFamily,
        bool activeOnly,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RagManualVersionInfo>>(
        [
            new RagManualVersionInfo("ECD-Manual-Mock", new DateOnly(2025, 3, 1), "Active"),
        ]);
}
