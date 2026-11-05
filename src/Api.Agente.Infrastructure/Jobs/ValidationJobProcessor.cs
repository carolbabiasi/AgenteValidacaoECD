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
        await db.SaveChangesAsync(cancellationToken);

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

            if (job.Execution is not null)
            {
                job.Execution.Status = ValidationStatus.Cancelled;
                job.Execution.ErrorCode = ExecutionCancelledException.Code;
                job.Execution.CompletedAt ??= DateTimeOffset.UtcNow;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar execução {ExecutionId}", job.ExecutionId);
            job.State = ValidationJobState.DeadLetter;
            job.LockedUntil = null;

            if (job.Execution is not null)
            {
                job.Execution.Status = ValidationStatus.Failed;
                job.Execution.ErrorCode = "PIPELINE_FAILED";
                job.Execution.ErrorMessage = ex.Message;
                job.Execution.CompletedAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
