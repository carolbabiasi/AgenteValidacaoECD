using Api.Agente.Domain.Abstractions;
using Api.Agente.Infrastructure.Persistence.Metrics;

namespace Api.Agente.Infrastructure.Metrics;

public sealed class LlmUsageRecorder(MetricsDbContext db) : ILlmUsageRecorder
{
    public async Task RecordAsync(LlmUsageEvent usageEvent, CancellationToken cancellationToken = default)
    {
        db.LlmUsageRecords.Add(new LlmUsageRecord
        {
            Id = Guid.NewGuid(),
            ExecutionId = usageEvent.ExecutionId,
            CorrelationId = usageEvent.CorrelationId,
            AttemptNumber = usageEvent.AttemptNumber,
            Service = "Api.Agente",
            Provider = "OpenAI",
            Model = usageEvent.Model,
            Operation = usageEvent.Operation,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            DurationMs = usageEvent.DurationMs,
            Status = usageEvent.Status,
            PromptTokens = usageEvent.PromptTokens,
            CompletionTokens = usageEvent.CompletionTokens,
            EstimatedCost = usageEvent.EstimatedCostUsd,
            IsEstimate = usageEvent.EstimatedCostUsd is null,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
