using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed class EcdAccountingIndex
{
    public EcdChartOfAccountsIndexView Chart { get; private init; } = null!;

    public EcdPeriodicBalanceIndex PeriodicBalances { get; private init; } = null!;

    public EcdJournalIndex Journal { get; private init; } = null!;

    public static EcdAccountingIndex Build(IReadOnlyList<EcdLine> lines)
    {
        var accounts = EcdChartOfAccountsIndex.Parse(lines);
        return new EcdAccountingIndex
        {
            Chart = EcdChartOfAccountsIndexView.FromEntries(accounts),
            PeriodicBalances = EcdPeriodicBalanceIndex.Build(lines),
            Journal = EcdJournalIndex.Build(lines),
        };
    }
}

public sealed class EcdChartOfAccountsIndexView
{
    private readonly Dictionary<string, EcdAccountEntry> _byCode;

    private EcdChartOfAccountsIndexView(Dictionary<string, EcdAccountEntry> byCode)
    {
        _byCode = byCode;
    }

    public IReadOnlyCollection<EcdAccountEntry> All => _byCode.Values;

    public static EcdChartOfAccountsIndexView FromEntries(IReadOnlyList<EcdAccountEntry> accounts)
    {
        var map = new Dictionary<string, EcdAccountEntry>(StringComparer.Ordinal);
        foreach (var entry in accounts)
        {
            map.TryAdd(entry.CodConta, entry);
        }

        return new EcdChartOfAccountsIndexView(map);
    }

    public bool ContainsAccount(string codConta) =>
        !string.IsNullOrWhiteSpace(codConta) && _byCode.ContainsKey(codConta.Trim());

    public EcdAccountEntry? TryGetAccount(string codConta) =>
        string.IsNullOrWhiteSpace(codConta) ? null : _byCode.GetValueOrDefault(codConta.Trim());

    public bool IsAnalytic(string codConta) =>
        TryGetAccount(codConta)?.Indicador?.Trim().ToUpperInvariant() == "A";

    public bool IsResultAccount(string codConta) =>
        TryGetAccount(codConta)?.Natureza?.Trim() == "04";
}
