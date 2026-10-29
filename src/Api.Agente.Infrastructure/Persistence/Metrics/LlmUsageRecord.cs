namespace Api.Agente.Infrastructure.Persistence.Metrics;

public sealed class LlmUsageRecord
{
    public Guid Id { get; set; }

    public Guid ExecutionId { get; set; }

    public string? CorrelationId { get; set; }

    public int AttemptNumber { get; set; }

    public required string Service { get; set; }

    public required string Provider { get; set; }

    public required string Model { get; set; }

    public required string Operation { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public int? DurationMs { get; set; }

    public required string Status { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? EmbeddingTokens { get; set; }

    public decimal? EstimatedCost { get; set; }

    public string? PriceTableRef { get; set; }

    public bool IsEstimate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
