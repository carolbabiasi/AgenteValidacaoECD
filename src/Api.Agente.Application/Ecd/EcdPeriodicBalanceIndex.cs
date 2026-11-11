using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed class EcdPeriodicBalanceIndex
{
    private readonly List<EcdPeriodicPeriod> _periods;
    private readonly List<EcdPeriodicBalanceLine> _balances;

    private EcdPeriodicBalanceIndex(List<EcdPeriodicPeriod> periods, List<EcdPeriodicBalanceLine> balances)
    {
        _periods = periods;
        _balances = balances;
    }

    public IReadOnlyList<EcdPeriodicPeriod> Periods => _periods;

    public IReadOnlyList<EcdPeriodicBalanceLine> Balances => _balances;

    public static EcdPeriodicBalanceIndex Build(IReadOnlyList<EcdLine> lines)
    {
        var periods = new List<EcdPeriodicPeriod>();
        var balances = new List<EcdPeriodicBalanceLine>();
        EcdPeriodicPeriod? current = null;

        foreach (var line in lines)
        {
            if (line.RecordType == "I150")
            {
                var dtIniText = EcdFieldParser.GetField(line, 2);
                var dtFinText = EcdFieldParser.GetField(line, 3);
                if (EcdFieldParser.TryParseSpedDate(dtIniText, out var dtIni) &&
                    EcdFieldParser.TryParseSpedDate(dtFinText, out var dtFin))
                {
                    current = new EcdPeriodicPeriod(line.LineNumber, dtIni, dtFin);
                    periods.Add(current);
                }
                else
                {
                    current = null;
                }

                continue;
            }

            if (line.RecordType != "I155" || current is null)
            {
                continue;
            }

            if (TryParseI155(line, current, out var balance))
            {
                balances.Add(balance);
            }
        }

        return new EcdPeriodicBalanceIndex(periods, balances);
    }

    public EcdPeriodicPeriod? FindPeriodContaining(DateOnly date) =>
        _periods.FirstOrDefault(p => date >= p.DtIni && date <= p.DtFin);

    public IEnumerable<EcdPeriodicBalanceLine> GetBalancesForPeriod(EcdPeriodicPeriod period) =>
        _balances.Where(b => b.Period.HeaderLineNumber == period.HeaderLineNumber);

    public EcdPeriodicBalanceLine? TryGetBalance(EcdPeriodicPeriod period, EcdAccountKey account) =>
        _balances.FirstOrDefault(b =>
            b.Period.HeaderLineNumber == period.HeaderLineNumber &&
            b.Account.CodConta == account.CodConta &&
            b.Account.CodCcus == account.CodCcus);

    internal static bool TryParseI155(EcdLine line, EcdPeriodicPeriod period, out EcdPeriodicBalanceLine balance)
    {
        balance = null!;

        var codConta = EcdFieldParser.GetField(line, 2)?.Trim();
        if (string.IsNullOrWhiteSpace(codConta))
        {
            return false;
        }

        var codCcus = EcdFieldParser.GetField(line, 3)?.Trim() ?? string.Empty;
        var account = EcdAccountKey.Create(codConta, codCcus);

        if (TryParseManualI155(line, period, account, out balance))
        {
            return true;
        }

        return TryParseFixtureI155(line, period, account, out balance);
    }

    private static bool TryParseManualI155(
        EcdLine line,
        EcdPeriodicPeriod period,
        EcdAccountKey account,
        out EcdPeriodicBalanceLine balance)
    {
        balance = null!;
        var indIni = EcdFieldParser.GetField(line, 5)?.Trim();
        var indFin = EcdFieldParser.GetField(line, 9)?.Trim();
        if (indIni is not ("D" or "C") || indFin is not ("D" or "C"))
        {
            return false;
        }

        if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 4), out var sldIni) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 6), out var deb) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 7), out var cred) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 8), out var sldFin))
        {
            return false;
        }

        balance = new EcdPeriodicBalanceLine(
            line.LineNumber,
            period,
            account,
            sldIni,
            indIni,
            deb,
            cred,
            sldFin,
            indFin);
        return true;
    }

    private static bool TryParseFixtureI155(
        EcdLine line,
        EcdPeriodicPeriod period,
        EcdAccountKey account,
        out EcdPeriodicBalanceLine balance)
    {
        balance = null!;
        var indIni = EcdFieldParser.GetField(line, 6)?.Trim();
        var indFin = EcdFieldParser.GetField(line, 9)?.Trim();
        if (indIni is not ("D" or "C") || indFin is not ("D" or "C"))
        {
            return false;
        }

        if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 4), out var sldIni) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 7), out var deb) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 8), out var cred))
        {
            return false;
        }

        var sldFin = sldIni;

        balance = new EcdPeriodicBalanceLine(
            line.LineNumber,
            period,
            account,
            sldIni,
            indIni,
            deb,
            cred,
            sldFin,
            indFin);
        return true;
    }
}
