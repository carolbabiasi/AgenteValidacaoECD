using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Api.Agente.Infrastructure.Persistence;

public sealed class ValidationExecutionRepository(OperationalDbContext db) : IValidationExecutionRepository
{
    public Task<ValidationExecution?> GetWithFileAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return db.ValidationExecutions
            .Include(e => e.OriginalFile)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
    }

    public Task<ValidationExecution?> GetWithFindingsAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        return db.ValidationExecutions
            .Include(e => e.Findings)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
    }

    public async Task SaveRuleResultsAndFindingsAsync(
        Guid executionId,
        IReadOnlyList<RuleRunResult> ruleResults,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default)
    {
        db.RuleRunResults.AddRange(ruleResults);
        db.Findings.AddRange(findings);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateProgressAsync(
        Guid executionId,
        ValidationStatus status,
        string? phase,
        int? percent,
        Action<ValidationExecution>? mutate = null,
        CancellationToken cancellationToken = default)
    {
        var execution = await db.ValidationExecutions.FirstAsync(e => e.Id == executionId, cancellationToken);
        execution.Status = status;
        execution.ProgressPhase = phase;
        execution.ProgressPercent = percent;
        mutate?.Invoke(execution);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetReportAsync(Guid executionId, string reportJson, CancellationToken cancellationToken = default)
    {
        var execution = await db.ValidationExecutions.FirstAsync(e => e.Id == executionId, cancellationToken);
        execution.ReportJson = reportJson;
        await db.SaveChangesAsync(cancellationToken);
    }
}
