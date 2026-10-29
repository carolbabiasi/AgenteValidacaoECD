using Api.Agente.Domain.Enums;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Domain.Rules;

public sealed class RuleEvaluationResult
{
    public required string RuleId { get; init; }

    public required string RuleVersion { get; init; }

    public RuleOutcome Outcome { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<RuleEvidence> Evidence { get; init; } = [];
}
