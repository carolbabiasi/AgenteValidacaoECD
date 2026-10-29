using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Entities;

public sealed class ValidationExecution
{
    public Guid Id { get; set; }

    public required string CorrelationId { get; set; }

    public ValidationStatus Status { get; set; }

    public required string SubmittedByUserId { get; set; }

    public string? OrganizationId { get; set; }

    public Guid OriginalFileId { get; set; }

    public StoredFile? OriginalFile { get; set; }

    public string? DetectedLayoutVersion { get; set; }

    public DateOnly? DetectedPeriodStart { get; set; }

    public DateOnly? DetectedPeriodEnd { get; set; }

    public int RecordsAnalyzedCount { get; set; }

    public int RulesExecutedCount { get; set; }

    public int FindingsCount { get; set; }

    public string? ManualVersionUsed { get; set; }

    public string? RagSnapshotJson { get; set; }

    public string? LlmResponseSchemaVersion { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ProgressPhase { get; set; }

    public int? ProgressPercent { get; set; }

    public string? ReportJson { get; set; }

    public ICollection<RuleRunResult> RuleRunResults { get; set; } = [];

    public ICollection<Finding> Findings { get; set; } = [];

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public ICollection<ValidationJob> Jobs { get; set; } = [];
}
