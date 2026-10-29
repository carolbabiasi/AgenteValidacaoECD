using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using System.Globalization;

namespace Api.Agente.Application.Ecd;

public sealed class BasicLayoutDetector : ILayoutDetector
{
    public LayoutDetectionResult Detect(IReadOnlyList<EcdLine> lines)
    {
        var opening = lines.FirstOrDefault(l => l.RecordType == "0000");
        if (opening is null)
        {
            return new LayoutDetectionResult(null, null, null, IsIndeterminate: true);
        }

        var fields = SplitFields(opening.RawText);
        string? layoutVersion = fields.Length > 2 && !string.IsNullOrWhiteSpace(fields[2])
            ? fields[2]
            : null;

        DateOnly? periodStart = TryParseDate(fields, 3);
        DateOnly? periodEnd = TryParseDate(fields, 4);

        var indeterminate = string.IsNullOrWhiteSpace(layoutVersion);
        return new LayoutDetectionResult(layoutVersion, periodStart, periodEnd, indeterminate);
    }

    private static string[] SplitFields(string raw)
    {
        return raw.Split('|', StringSplitOptions.None);
    }

    private static DateOnly? TryParseDate(string[] fields, int index)
    {
        if (fields.Length <= index)
        {
            return null;
        }

        var value = fields[index];
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(value, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return d;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
        {
            return d;
        }

        return null;
    }
}
