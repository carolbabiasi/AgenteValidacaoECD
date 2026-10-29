using Api.Agente.Application.Reports;
using Api.Agente.Domain.Abstractions;

namespace Api.Agente.Tests.Unit;

public sealed class ReportExportServiceTests
{
    private readonly IReportExportService _service = new ReportExportService();

    [Fact]
    public void Export_Csv_IncludesFindingColumns()
    {
        const string report = """
            {
              "findings": [
                {
                  "findingId": "abc",
                  "severity": "High",
                  "category": "StructuralError",
                  "title": "Bloco ausente",
                  "description": "Detalhe",
                  "recordRefs": [{ "recordType": "0000", "lineNumber": 1 }],
                  "ruleId": "SAMPLE-MISSING-OPENING"
                }
              ]
            }
            """;

        var result = _service.Export(report, "csv");
        var text = System.Text.Encoding.UTF8.GetString(result.Content);

        Assert.Contains("FindingId", text, StringComparison.Ordinal);
        Assert.Contains("abc", text, StringComparison.Ordinal);
        Assert.Contains("0000", text, StringComparison.Ordinal);
        Assert.Contains("SAMPLE-MISSING-OPENING", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_Xlsx_ReturnsOpenXmlPayload()
    {
        const string report = """{"findings":[{"findingId":"x","severity":"Low","category":"AiIndication","title":"t","description":"d","recordRefs":[],"ruleId":"r"}]}""";

        var result = _service.Export(report, "xlsx");

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.ContentType);
        Assert.True(result.Content.Length > 100);
        Assert.StartsWith("PK", System.Text.Encoding.UTF8.GetString(result.Content.AsSpan(0, 2)));
    }
}
