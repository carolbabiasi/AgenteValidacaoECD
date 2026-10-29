using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Entities;

public sealed class RuleRunResult
{
    public Guid Id { get; set; }

    public Guid ExecutionId { get; set; }

    public ValidationExecution? Execution { get; set; }

    public required string RuleId { get; set; }

    public required string RuleVersion { get; set; }

    public RuleOutcome Outcome { get; set; }

    public required string EvidenceJson { get; set; }

    public required string Message { get; set; }
}
