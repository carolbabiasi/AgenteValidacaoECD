namespace Api.Agente.Domain.Abstractions;

public interface IReportExportService
{
    ReportExportResult Export(string reportJson, string format);
}

public sealed record ReportExportResult(
    byte[] Content,
    string ContentType,
    string FileName);
