namespace Api.Agente.Domain.Abstractions;

public sealed record LlmUsageEvent(
    Guid ExecutionId,
    string? CorrelationId,
    int AttemptNumber,
    string Operation,
    string Model,
    string Status,
    int? PromptTokens,
    int? CompletionTokens,
    int DurationMs,
    decimal? EstimatedCostUsd);

public interface ILlmUsageRecorder
{
    Task RecordAsync(LlmUsageEvent usageEvent, CancellationToken cancellationToken = default);
}
