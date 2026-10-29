using Api.Rag.Domain.Abstractions;
using Api.Rag.Domain.Enums;
using Api.Rag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Rag.Infrastructure.Jobs;

public sealed class IngestionJobProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<IngestionJobProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

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
                logger.LogError(ex, "Erro no processador de ingestão RAG");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<RagDbContext>();
        var pipeline = scope.ServiceProvider.GetRequiredService<IManualIngestionPipeline>();

        var job = await db.IngestionJobs
            .Where(j => j.Status == IngestionJobStatus.Pending)
            .OrderBy(j => j.StartedAt ?? DateTimeOffset.MinValue)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return;
        }

        try
        {
            await pipeline.RunAsync(job.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Job de ingestão {JobId} falhou", job.Id);
        }
    }
}
