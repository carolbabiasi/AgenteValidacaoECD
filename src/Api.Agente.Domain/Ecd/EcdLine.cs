namespace Api.Agente.Domain.Ecd;

public sealed record EcdLine(int LineNumber, string RawText, string? RecordType);
