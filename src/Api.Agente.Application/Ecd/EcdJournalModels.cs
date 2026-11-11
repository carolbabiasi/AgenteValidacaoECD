namespace Api.Agente.Application.Ecd;

public sealed record EcdJournalHeader(
    int LineNumber,
    string NumeroLancamento,
    DateOnly DataLancamento,
    string? IndicadorLancamento,
    decimal? ValorLancamento);

public sealed record EcdJournalPartLine(
    int LineNumber,
    EcdJournalHeader Header,
    EcdAccountKey Account,
    decimal Valor,
    string IndicadorDc);
