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

    public async Task<ValidationHistoryPage> ListHistoryAsync(
        ValidationHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<ValidationExecution> executions = db.ValidationExecutions
            .AsNoTracking()
            .Include(e => e.OriginalFile);

        if (!query.IsAdmin)
        {
            executions = executions.Where(e => e.SubmittedByUserId == query.UserId);
        }

        if (query.From.HasValue)
        {
            executions = executions.Where(e => e.SubmittedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            executions = executions.Where(e => e.SubmittedAt <= query.To.Value);
        }

        if (query.Status.HasValue)
        {
            executions = executions.Where(e => e.Status == query.Status.Value);
        }

        var totalCount = await executions.CountAsync(cancellationToken);

        var rows = await executions
            .OrderByDescending(e => e.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(e => new ValidationHistoryItem(
                e.Id,
                e.SubmittedAt,
                e.OriginalFile?.OriginalFileName,
                e.Status,
                e.FindingsCount,
                e.DetectedLayoutVersion))
            .ToList();

        return new ValidationHistoryPage(page, pageSize, totalCount, items);
    }
}
