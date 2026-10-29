using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Abstractions;

public interface IValidationExecutionRepository
{
    Task<ValidationExecution?> GetWithFileAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<ValidationExecution?> GetWithFindingsAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task SaveRuleResultsAndFindingsAsync(
        Guid executionId,
        IReadOnlyList<RuleRunResult> ruleResults,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default);

    Task UpdateProgressAsync(
        Guid executionId,
        ValidationStatus status,
        string? phase,
        int? percent,
        Action<ValidationExecution>? mutate = null,
        CancellationToken cancellationToken = default);

    Task SetReportAsync(Guid executionId, string reportJson, CancellationToken cancellationToken = default);

    Task<ValidationHistoryPage> ListHistoryAsync(
        ValidationHistoryQuery query,
        CancellationToken cancellationToken = default);
}
