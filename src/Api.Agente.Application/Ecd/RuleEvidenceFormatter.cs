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
        if (IsCounterEvidence(item))
        {
            return FormatCounterLine(item);
        }

        var message = item.Message?.Trim() ?? string.Empty;

        if (MessageMentionsLine(message, item.LineNumber))
        {
            return message;
        }

        var head = BuildLocationHead(item);
        if (string.IsNullOrWhiteSpace(message))
        {
            return AppendResidualValues(head, item);
        }

        return string.IsNullOrWhiteSpace(head) ? message : $"{head}: {message}";
    }

    private static bool IsCounterEvidence(RuleEvidence item) =>
        item.RecordType is "I990" or "9999"
        && item.FieldName is "QTD_LIN" or "QTD_LIN_ARQ";

    private static string FormatCounterLine(RuleEvidence item)
    {
        var prefix = item.LineNumber is > 0 ? $"Linha {item.LineNumber} · " : string.Empty;
        var found = item.FoundValue ?? "—";
        var expected = item.ExpectedValue ?? "—";

        return item.RecordType switch
        {
            "I990" =>
                $"{prefix}Encerramento do Bloco I (I990): quantidade declarada é {found}, mas o bloco contém {expected} registros.",
            "9999" =>
                $"{prefix}Encerramento do arquivo (9999): contador aponta {found} linhas; o arquivo possui {expected} linhas.",
            _ => $"{prefix}{item.Message}".Trim(),
        };
    }

    private static string BuildLocationHead(RuleEvidence item)
    {
        if (item.LineNumber is not > 0)
        {
            return item.RecordType ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(item.RecordType))
        {
            return $"Linha {item.LineNumber.Value}";
        }

        return $"Linha {item.LineNumber.Value} · {item.RecordType}";
    }

    private static bool MessageMentionsLine(string message, int? lineNumber) =>
        lineNumber is > 0
        && message.Contains($"linha {lineNumber.Value}", StringComparison.OrdinalIgnoreCase);

    private static string AppendResidualValues(string head, RuleEvidence item)
    {
        if (string.IsNullOrWhiteSpace(head)
            && string.IsNullOrWhiteSpace(item.FoundValue)
            && string.IsNullOrWhiteSpace(item.ExpectedValue))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(head);
        if (!string.IsNullOrWhiteSpace(item.FoundValue))
        {
            sb.Append(sb.Length > 0 ? " — valor informado: " : "Valor informado: ");
            sb.Append(item.FoundValue);
        }

        if (!string.IsNullOrWhiteSpace(item.ExpectedValue))
        {
            sb.Append(sb.Length > 0 ? "; correto seria " : "Correto seria ");
            sb.Append(item.ExpectedValue);
            sb.Append('.');
        }

        return sb.ToString();
    }
}
