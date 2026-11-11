using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed record EcdClosingApuration(int LineNumber, DateOnly DtRes);

public sealed record EcdClosingAccountBalance(
    int LineNumber,
    EcdClosingApuration Apuration,
    EcdAccountKey Account,
    decimal Valor,
    string IndicadorDc);

public sealed class EcdClosingIndex
{
    private readonly List<EcdClosingApuration> _apuracoes;
    private readonly List<EcdClosingAccountBalance> _balances;

    private EcdClosingIndex(
        List<EcdClosingApuration> apuracoes,
        List<EcdClosingAccountBalance> balances)
    {
        _apuracoes = apuracoes;
        _balances = balances;
    }

    public IReadOnlyList<EcdClosingApuration> Apuracoes => _apuracoes;

    public IReadOnlyList<EcdClosingAccountBalance> Balances => _balances;

    public static EcdClosingIndex Build(IReadOnlyList<EcdLine> lines)
    {
        var apuracoes = new List<EcdClosingApuration>();
        var balances = new List<EcdClosingAccountBalance>();
        EcdClosingApuration? current = null;

        foreach (var line in lines)
        {
            if (line.RecordType is "I990")
            {
                current = null;
                continue;
            }

            if (line.RecordType == "I350")
            {
                if (TryParseI350(line, out current))
                {
                    apuracoes.Add(current);
                }
                else
                {
                    current = null;
                }

                continue;
            }

            if (line.RecordType != "I355" || current is null)
            {
                continue;
            }

            if (TryParseI355(line, current, out var balance))
            {
                balances.Add(balance);
            }
        }

        return new EcdClosingIndex(apuracoes, balances);
    }

    private static bool TryParseI350(EcdLine line, out EcdClosingApuration apuracao)
    {
        apuracao = null!;
        var dtText = EcdFieldParser.GetField(line, 2);
        if (!EcdFieldParser.TryParseSpedDate(dtText, out var dtRes))
        {
            return false;
        }

        apuracao = new EcdClosingApuration(line.LineNumber, dtRes);
        return true;
    }

    private static bool TryParseI355(
        EcdLine line,
        EcdClosingApuration apuracao,
        out EcdClosingAccountBalance balance)
    {
        balance = null!;

        var codConta = EcdFieldParser.GetField(line, 2)?.Trim();
        if (string.IsNullOrWhiteSpace(codConta))
        {
            return false;
        }

        var codCcus = EcdFieldParser.GetField(line, 3)?.Trim() ?? string.Empty;
        var ind = EcdFieldParser.GetField(line, 5)?.Trim().ToUpperInvariant();
        if (ind is not ("D" or "C"))
        {
            return false;
        }

        if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 4), out var valor))
        {
            return false;
        }

        balance = new EcdClosingAccountBalance(
            line.LineNumber,
            apuracao,
            EcdAccountKey.Create(codConta, codCcus),
            valor,
            ind);
        return true;
    }
}
