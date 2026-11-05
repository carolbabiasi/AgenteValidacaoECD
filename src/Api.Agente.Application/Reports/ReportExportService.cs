using System.Globalization;
using System.Text;
using System.Text.Json;
using Api.Agente.Domain.Abstractions;
using ClosedXML.Excel;

namespace Api.Agente.Application.Reports;

public sealed class ReportExportService : IReportExportService
{
    private static readonly string[] Headers =
    [
        "FindingId",
        "Severity",
        "Category",
        "Title",
        "Description",
        "RecordType",
        "LineNumber",
        "RuleId",
    ];

    public ReportExportResult Export(string reportJson, string format)
    {
        var rows = ParseFindings(reportJson);
        var normalized = format.Trim().ToLowerInvariant();

        return normalized switch
        {
            "csv" => ExportCsv(rows),
            "xlsx" => ExportXlsx(rows),
            _ => throw new ArgumentException("INVALID_FORMAT: use csv ou xlsx.", nameof(format)),
        };
    }

    private static List<string?[]> ParseFindings(string reportJson)
    {
        using var document = JsonDocument.Parse(reportJson);
        if (!document.RootElement.TryGetProperty("findings", out var findings) ||
            findings.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rows = new List<string?[]>();
        foreach (var finding in findings.EnumerateArray())
        {
            var recordType = (string?)null;
            int? lineNumber = null;
            if (finding.TryGetProperty("recordRefs", out var refs) && refs.ValueKind == JsonValueKind.Array)
            {
                foreach (var reference in refs.EnumerateArray())
                {
                    if (reference.TryGetProperty("recordType", out var rt) && rt.ValueKind == JsonValueKind.String)
                    {
                        recordType = rt.GetString();
                    }

                    if (reference.TryGetProperty("lineNumber", out var ln) && ln.TryGetInt32(out var line))
                    {
                        lineNumber = line;
                    }

                    break;
                }
            }

            rows.Add(
            [
                GetString(finding, "findingId"),
                GetString(finding, "severity"),
                GetString(finding, "category"),
                GetString(finding, "title"),
                GetString(finding, "description"),
                recordType,
                lineNumber?.ToString(CultureInfo.InvariantCulture),
                GetString(finding, "ruleId"),
            ]);
        }

        return rows;
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static ReportExportResult ExportCsv(List<string?[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Headers.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', row.Select(v => EscapeCsv(v ?? string.Empty))));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return new ReportExportResult(bytes, "text/csv; charset=utf-8", "ecd-findings.csv");
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private static ReportExportResult ExportXlsx(List<string?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Findings");

        for (var c = 0; c < Headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = Headers[c];
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Length; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = row[c] ?? string.Empty;
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ReportExportResult(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ecd-findings.xlsx");
    }
}
