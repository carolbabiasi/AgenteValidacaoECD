using System.Text;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Ecd;

public static class RuleEvidenceFormatter
{
    public const char SofiaEvidenceLineSeparator = '\n';

    public static IReadOnlyList<string> ToDisplayLines(IReadOnlyList<RuleEvidence> evidence)
    {
        if (evidence.Count == 0)
        {
            return [];
        }

        var lines = new List<string>(evidence.Count);
        foreach (var item in evidence)
        {
            var line = FormatLine(item);
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    public static string ToExplanationStorage(IReadOnlyList<RuleEvidence> evidence)
    {
        var lines = ToDisplayLines(evidence);
        return lines.Count switch
        {
            0 => string.Empty,
            1 => lines[0],
            _ => string.Join(SofiaEvidenceLineSeparator, lines),
        };
    }

    public static IReadOnlyList<string> ParseExplanationLines(string? explanation)
    {
        if (string.IsNullOrWhiteSpace(explanation))
        {
            return [];
        }

        return explanation
            .Split(SofiaEvidenceLineSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string FormatLine(RuleEvidence item)
    {
        var sb = new StringBuilder();

        if (item.LineNumber is > 0)
        {
            sb.Append($"Linha {item.LineNumber.Value}");
        }

        if (!string.IsNullOrWhiteSpace(item.RecordType))
        {
            sb.Append(sb.Length > 0 ? $" — {item.RecordType}" : item.RecordType);
        }

        if (!string.IsNullOrWhiteSpace(item.FieldName))
        {
            sb.Append($" (campo {item.FieldName})");
        }

        if (!string.IsNullOrWhiteSpace(item.Message))
        {
            sb.Append(sb.Length > 0 ? $": {item.Message}" : item.Message);
        }

        if (!string.IsNullOrWhiteSpace(item.FoundValue))
        {
            sb.Append($" — valor: {item.FoundValue}");
        }

        if (!string.IsNullOrWhiteSpace(item.ExpectedValue))
        {
            sb.Append($" — esperado: {item.ExpectedValue}");
        }

        return sb.ToString();
    }
}
