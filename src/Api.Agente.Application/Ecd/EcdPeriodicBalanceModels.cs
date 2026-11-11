namespace Api.Agente.Application.Ecd;

public sealed record EcdPeriodicPeriod(
    int HeaderLineNumber,
    DateOnly DtIni,
    DateOnly DtFin);

public sealed record EcdPeriodicBalanceLine(
    int LineNumber,
    EcdPeriodicPeriod Period,
    EcdAccountKey Account,
    decimal SaldoInicial,
    string? IndicadorSaldoInicial,
    decimal TotalDebitos,
    decimal TotalCreditos,
    decimal SaldoFinal,
    string? IndicadorSaldoFinal);
