using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed record EcdRecuperacaoPeriod(int LineNumber, DateOnly DtIni, DateOnly DtFin);

public sealed record EcdRecuperacaoBalanceLine(
    int LineNumber,
    EcdRecuperacaoPeriod? Period,
    EcdAccountKey Account,
    decimal SaldoInicial,
    string? IndicadorSaldoInicial,
    decimal TotalDebitos,
    decimal TotalCreditos,
    decimal SaldoFinal,
    string? IndicadorSaldoFinal);

public sealed class EcdRecuperacaoC155Index
{
    private readonly List<EcdRecuperacaoBalanceLine> _balances;

    private EcdRecuperacaoC155Index(List<EcdRecuperacaoBalanceLine> balances) =>
        _balances = balances;

    public IReadOnlyList<EcdRecuperacaoBalanceLine> Balances => _balances;

    public bool HasRecuperacao => _balances.Count > 0;

    public EcdRecuperacaoBalanceLine? TryGet(EcdAccountKey account) =>
        _balances.FirstOrDefault(b =>
            b.Account.CodConta == account.CodConta &&
            b.Account.CodCcus == account.CodCcus);

    public static EcdRecuperacaoC155Index Build(IReadOnlyList<EcdLine> lines)
    {
        var balances = new List<EcdRecuperacaoBalanceLine>();
        EcdRecuperacaoPeriod? current = null;

        foreach (var line in lines)
        {
            if (line.RecordType == "C150")
            {
                if (EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(line, 2), out var dtIni) &&
                    EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(line, 3), out var dtFin))
                {
                    current = new EcdRecuperacaoPeriod(line.LineNumber, dtIni, dtFin);
                }
                else
                {
                    current = null;
                }

                continue;
            }

            if (line.RecordType != "C155")
            {
                if (line.RecordType is "I001" or "I010")
                {
                    current = null;
                }

                continue;
            }

            if (TryParseC155(line, current, out var balance))
            {
                balances.Add(balance);
            }
        }

        return new EcdRecuperacaoC155Index(balances);
    }

    private static bool TryParseC155(
        EcdLine line,
        EcdRecuperacaoPeriod? period,
        out EcdRecuperacaoBalanceLine balance)
    {
        balance = null!;
        var codConta = EcdFieldParser.GetField(line, 2)?.Trim();
        if (string.IsNullOrWhiteSpace(codConta))
        {
            return false;
        }

        var codCcus = EcdFieldParser.GetField(line, 3)?.Trim() ?? string.Empty;
        var account = EcdAccountKey.Create(codConta, codCcus);

        if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 4), out var sldIni) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 6), out var deb) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 7), out var cred) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 8), out var sldFin))
        {
            return false;
        }

        balance = new EcdRecuperacaoBalanceLine(
            line.LineNumber,
            period,
            account,
            sldIni,
            EcdFieldParser.GetField(line, 5)?.Trim(),
            deb,
            cred,
            sldFin,
            EcdFieldParser.GetField(line, 9)?.Trim());
        return true;
    }
}
