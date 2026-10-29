using Api.Agente.Domain.Entities;

namespace Api.Agente.Domain.Abstractions;

public sealed record AgentAnalysisResult(
    string ReportJson,
    string? ManualVersionUsed,
    string RagSnapshotJson,
    int AttemptsUsed);

public interface IAgentAnalysisService
{
    Task<AgentAnalysisResult> EnrichReportAsync(
        ValidationExecution execution,
        string deterministicReportJson,
        RagContextSnapshot ragContext,
        CancellationToken cancellationToken = default);
}
