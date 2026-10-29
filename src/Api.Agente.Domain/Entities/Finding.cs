using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Entities;

public sealed class Finding
{
    public Guid Id { get; set; }

    public Guid ExecutionId { get; set; }

    public ValidationExecution? Execution { get; set; }

    public FindingCategory Category { get; set; }

    public FindingSeverity Severity { get; set; }

    public string? PrimaryRecordType { get; set; }

    public int? LineNumber { get; set; }

    public required string Summary { get; set; }

    public string? Explanation { get; set; }

    public required string SourceRuleRunIdsJson { get; set; }
}
