namespace Api.Agente.Domain.ValueObjects;

public sealed class RuleEvidence
{
    public string? RecordType { get; init; }

    public int? LineNumber { get; init; }

    public string? FieldName { get; init; }

    public string? ExpectedValue { get; init; }

    public string? FoundValue { get; init; }

    public string? Message { get; init; }
}
