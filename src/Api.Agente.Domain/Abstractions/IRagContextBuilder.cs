using Api.Agente.Domain.Entities;

namespace Api.Agente.Domain.Abstractions;

public sealed record RagContextSnapshot(
    string? ManualVersionResolved,
    string LayoutFamily,
    IReadOnlyList<RagManualSearchHit> EvidenceHits,
    IReadOnlyList<string> Limitations);

public interface IRagContextBuilder
{
    Task<RagContextSnapshot> BuildAsync(
        ValidationExecution execution,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default);
}
