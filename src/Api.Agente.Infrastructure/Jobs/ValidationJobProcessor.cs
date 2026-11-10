using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Exceptions;
using Api.Agente.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Infrastructure.Jobs;

public sealed class ValidationJobProcessor(
    IServiceScopeFactory scopeFactory,
    ExecutionCancellationRegistry cancellationRegistry,
    ILogger<ValidationJobProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erro no processador de jobs de validação");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperationalDbContext>();
        var pipeline = scope.ServiceProvider.GetRequiredService<IValidationPipeline>();

        var now = DateTimeOffset.UtcNow;
        var job = await db.ValidationJobs
            .Include(j => j.Execution)
            .Where(j => j.State == ValidationJobState.Pending &&
                        (j.LockedUntil == null || j.LockedUntil < now))
            .OrderBy(j => j.Execution!.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return;
        }

        job.State = ValidationJobState.Processing;
        job.LockedUntil = now.Add(LockDuration);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogDebug("Job {JobId} já reivindicado por outro worker", job.Id);
            return;
        }

        try
        {
            using var executionScope = cancellationRegistry.Register(job.ExecutionId, cancellationToken);
            await pipeline.RunAsync(job.ExecutionId, executionScope.Token);
            job.State = ValidationJobState.Done;
            job.LockedUntil = null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Execução {ExecutionId} interrompida por cancelamento", job.ExecutionId);
            job.State = ValidationJobState.Cancelled;
            job.LockedUntil = null;

            var statusAtCancel = await db.ValidationExecutions
                .AsNoTracking()
                .Where(e => e.Id == job.ExecutionId)
                .Select(e => e.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (job.Execution is not null && !IsTerminalExecutionStatus(statusAtCancel))
            {
                job.Execution.Status = ValidationStatus.Cancelled;
                job.Execution.ErrorCode = ExecutionCancelledException.Code;
                job.Execution.CompletedAt ??= DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar execução {ExecutionId}", job.ExecutionId);
            job.LockedUntil = null;

            var persistedStatus = await db.ValidationExecutions
                .AsNoTracking()
                .Where(e => e.Id == job.ExecutionId)
                .Select(e => e.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (IsTerminalExecutionStatus(persistedStatus))
            {
                logger.LogWarning(
                    "Execução {ExecutionId} já em estado terminal ({Status}); outro worker tratou",
                    job.ExecutionId,
                    persistedStatus);
                job.State = ValidationJobState.Done;
            }
            else
            {
                job.State = ValidationJobState.DeadLetter;

                if (job.Execution is not null)
                {
                    job.Execution.Status = ValidationStatus.Failed;
                    job.Execution.ErrorCode = "PIPELINE_FAILED";
                    job.Execution.ErrorMessage = ex.Message;
                    job.Execution.CompletedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogDebug(
                "Persistência do job {JobId} (execução {ExecutionId}) ignorada: outro worker atualizou primeiro",
                job.Id,
                job.ExecutionId);
        }
    }

    private static bool IsTerminalExecutionStatus(ValidationStatus status) =>
        status is ValidationStatus.Completed or ValidationStatus.Failed or ValidationStatus.Cancelled;
}
