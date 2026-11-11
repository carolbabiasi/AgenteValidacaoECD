using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed record EcdI157TransferLine(
    int LineNumber,
    int ParentI155LineNumber,
    EcdPeriodicPeriod Period,
    EcdAccountKey ParentAccount,
    EcdAccountKey OldAccount,
    decimal SaldoInicial,
    string? IndicadorSaldoInicial);

public sealed class EcdPeriodicTransferIndex
{
    private readonly List<EcdI157TransferLine> _transfers;

    private EcdPeriodicTransferIndex(List<EcdI157TransferLine> transfers) =>
        _transfers = transfers;

    public IReadOnlyList<EcdI157TransferLine> Transfers => _transfers;

    public bool HasAnyTransfer => _transfers.Count > 0;

    public IEnumerable<EcdI157TransferLine> GetTransfersForI155(int i155LineNumber) =>
        _transfers.Where(t => t.ParentI155LineNumber == i155LineNumber);

    public static EcdPeriodicTransferIndex Build(IReadOnlyList<EcdLine> lines)
    {
        var transfers = new List<EcdI157TransferLine>();
        var balanceIndex = EcdPeriodicBalanceIndex.Build(lines);
        var balanceByLine = balanceIndex.Balances.ToDictionary(b => b.LineNumber);

        EcdPeriodicBalanceLine? parentI155 = null;
        foreach (var line in lines)
        {
            if (line.RecordType == "I150")
            {
                parentI155 = null;
                continue;
            }

            if (line.RecordType == "I155")
            {
                parentI155 = balanceByLine.GetValueOrDefault(line.LineNumber);
                continue;
            }

            if (line.RecordType != "I157" || parentI155 is null)
            {
                if (line.RecordType is "I200" or "I350" or "I990")
                {
                    parentI155 = null;
                }

                continue;
            }

            if (TryParseI157(line, parentI155, out var transfer))
            {
                transfers.Add(transfer);
            }
        }

        return new EcdPeriodicTransferIndex(transfers);
    }

    private static bool TryParseI157(
        EcdLine line,
        EcdPeriodicBalanceLine parent,
        out EcdI157TransferLine transfer)
    {
        transfer = null!;
        var codConta = EcdFieldParser.GetField(line, 2)?.Trim();
        if (string.IsNullOrWhiteSpace(codConta))
        {
            return false;
        }

        var codCcus = EcdFieldParser.GetField(line, 3)?.Trim() ?? string.Empty;
        if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, 4), out var sldIni))
        {
            return false;
        }

        var ind = EcdFieldParser.GetField(line, 5)?.Trim().ToUpperInvariant();
        transfer = new EcdI157TransferLine(
            line.LineNumber,
            parent.LineNumber,
            parent.Period,
            parent.Account,
            EcdAccountKey.Create(codConta, codCcus),
            sldIni,
            ind is "D" or "C" ? ind : null);
        return true;
    }
}
