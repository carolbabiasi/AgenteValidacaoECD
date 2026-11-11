using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed class EcdJournalIndex
{
    private readonly List<EcdJournalHeader> _headers;
    private readonly List<EcdJournalPartLine> _parts;
    private readonly Dictionary<int, EcdJournalHeader> _headerByLine;

    private EcdJournalIndex(
        List<EcdJournalHeader> headers,
        List<EcdJournalPartLine> parts,
        Dictionary<int, EcdJournalHeader> headerByLine)
    {
        _headers = headers;
        _parts = parts;
        _headerByLine = headerByLine;
    }

    public IReadOnlyList<EcdJournalHeader> Headers => _headers;

    public IReadOnlyList<EcdJournalPartLine> Parts => _parts;

    public static EcdJournalIndex Build(IReadOnlyList<EcdLine> lines)
    {
        var headers = new List<EcdJournalHeader>();
        var parts = new List<EcdJournalPartLine>();
        var headerByLine = new Dictionary<int, EcdJournalHeader>();
        EcdJournalHeader? current = null;

        foreach (var line in lines)
        {
            if (line.RecordType is "I990")
            {
                current = null;
                continue;
            }

            if (line.RecordType == "I200")
            {
                if (TryParseI200(line, out current))
                {
                    headers.Add(current);
                    headerByLine[line.LineNumber] = current;
                }
                else
                {
                    current = null;
                }

                continue;
            }

            if (line.RecordType != "I250" || current is null)
            {
                continue;
            }

            if (TryParseI250(line, current, out var part))
            {
                parts.Add(part);
            }
        }

        AttachComputedValorLancamento(headers, parts);
        return new EcdJournalIndex(headers, parts, headerByLine);
    }

    public EcdJournalHeader? TryGetHeaderAtLine(int lineNumber) =>
        _headerByLine.GetValueOrDefault(lineNumber);

    public IEnumerable<EcdJournalPartLine> GetParts(EcdJournalHeader header) =>
        _parts.Where(p => p.Header.LineNumber == header.LineNumber);

    public (decimal Debits, decimal Credits) SumParts(EcdJournalHeader header)
    {
        decimal debits = 0;
        decimal credits = 0;
        foreach (var part in GetParts(header))
        {
            if (part.IndicadorDc == "D")
            {
                debits += part.Valor;
            }
            else if (part.IndicadorDc == "C")
            {
                credits += part.Valor;
            }
        }

        return (debits, credits);
    }

    public decimal SumEncerramentoLancamentos(DateOnly dtRes, EcdAccountKey account)
    {
        decimal sum = 0;

        foreach (var header in _headers)
        {
            if (!string.Equals(header.IndicadorLancamento, "E", StringComparison.Ordinal) ||
                header.DataLancamento != dtRes)
            {
                continue;
            }

            foreach (var part in GetParts(header))
            {
                if (part.Account.CodConta != account.CodConta || part.Account.CodCcus != account.CodCcus)
                {
                    continue;
                }

                sum += EcdSignedAmount.FromEncerramentoLancamento(part.Valor, part.IndicadorDc);
            }
        }

        return sum;
    }

    public bool HasEncerramentoLancamentoOn(DateOnly dtRes) =>
        _headers.Any(h =>
            string.Equals(h.IndicadorLancamento, "E", StringComparison.Ordinal) &&
            h.DataLancamento == dtRes);

    public (decimal Debits, decimal Credits) SumPartsByAccount(
        EcdPeriodicPeriod period,
        EcdAccountKey account,
        EcdPeriodicBalanceIndex balances)
    {
        decimal debits = 0;
        decimal credits = 0;

        foreach (var header in _headers)
        {
            var date = header.DataLancamento;
            var periodForDate = balances.FindPeriodContaining(date);
            if (periodForDate?.HeaderLineNumber != period.HeaderLineNumber)
            {
                continue;
            }

            foreach (var part in GetParts(header))
            {
                if (part.Account.CodConta != account.CodConta || part.Account.CodCcus != account.CodCcus)
                {
                    continue;
                }

                if (part.IndicadorDc == "D")
                {
                    debits += part.Valor;
                }
                else if (part.IndicadorDc == "C")
                {
                    credits += part.Valor;
                }
            }
        }

        return (debits, credits);
    }

    private static void AttachComputedValorLancamento(List<EcdJournalHeader> headers, List<EcdJournalPartLine> parts)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            if (header.ValorLancamento.HasValue)
            {
                continue;
            }

            var headerParts = parts.Where(p => p.Header.LineNumber == header.LineNumber).ToList();
            if (headerParts.Count == 0)
            {
                continue;
            }

            var debits = headerParts.Where(p => p.IndicadorDc == "D").Sum(p => p.Valor);
            var credits = headerParts.Where(p => p.IndicadorDc == "C").Sum(p => p.Valor);
            var vl = Math.Max(debits, credits);
            headers[i] = header with { ValorLancamento = vl };
        }
    }

    private static bool TryParseI200(EcdLine line, out EcdJournalHeader header)
    {
        header = null!;

        var field2 = EcdFieldParser.GetField(line, 2)?.Trim() ?? string.Empty;
        var field3 = EcdFieldParser.GetField(line, 3)?.Trim() ?? string.Empty;

        DateOnly data;
        string numero;
        if (EcdFieldParser.TryParseSpedDate(field3, out data) && !EcdFieldParser.TryParseSpedDate(field2, out _))
        {
            numero = field2;
        }
        else if (EcdFieldParser.TryParseSpedDate(field2, out data))
        {
            numero = field3;
        }
        else
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(numero))
        {
            numero = $"L{line.LineNumber}";
        }

        decimal? valor = null;
        var vlText = EcdFieldParser.GetField(line, 4);
        var vlIsNumeric = EcdFieldParser.TryParseSpedDecimal(vlText, out var vl);
        if (vlIsNumeric)
        {
            valor = vl;
        }

        var indLcto = EcdFieldParser.GetField(line, 5)?.Trim().ToUpperInvariant();
        if (!vlIsNumeric)
        {
            indLcto ??= EcdFieldParser.GetField(line, 4)?.Trim().ToUpperInvariant();
        }

        if (indLcto is not ("N" or "E" or "X"))
        {
            indLcto = "N";
        }

        header = new EcdJournalHeader(line.LineNumber, numero, data, indLcto, valor);
        return true;
    }

    private static bool TryParseI250(EcdLine line, EcdJournalHeader header, out EcdJournalPartLine part)
    {
        part = null!;
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

        part = new EcdJournalPartLine(
            line.LineNumber,
            header,
            EcdAccountKey.Create(codConta, codCcus),
            valor,
            ind);
        return true;
    }
}
