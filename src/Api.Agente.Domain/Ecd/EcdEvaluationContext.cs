namespace Api.Agente.Domain.Ecd;

public sealed class EcdEvaluationContext
{
    public required Guid ExecutionId { get; init; }

    public string? LayoutVersion { get; init; }

    public IReadOnlyList<EcdLine> Lines { get; init; } = [];

    public int RecordsAnalyzedCount => Lines.Count;

    public bool HasOpeningBlock { get; init; }
}
