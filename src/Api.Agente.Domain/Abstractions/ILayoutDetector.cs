using Api.Agente.Domain.Ecd;

namespace Api.Agente.Domain.Abstractions;

public sealed record LayoutDetectionResult(
    string? LayoutVersion,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    bool IsIndeterminate);

public interface ILayoutDetector
{
    LayoutDetectionResult Detect(IReadOnlyList<EcdLine> lines);
}
