using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed record EcdAccountEntry(
    int LineNumber,
    string CodConta,
    string? CodContaSup,
    string? Natureza,
    string? Indicador);

public static class EcdChartOfAccountsIndex
{
    private static readonly HashSet<string> ValidNaturezas = new(StringComparer.Ordinal)
    {
        "01", "02", "03", "04", "05",
    };

    public static IReadOnlyList<EcdAccountEntry> Parse(IReadOnlyList<EcdLine> lines)
    {
        var accounts = new List<EcdAccountEntry>();
        foreach (var line in lines)
        {
            if (line.RecordType != "I050")
            {
                continue;
            }

            var cod = EcdFieldParser.GetField(line, 6)?.Trim();
            if (string.IsNullOrWhiteSpace(cod))
            {
                continue;
            }

            accounts.Add(new EcdAccountEntry(
                line.LineNumber,
                cod,
                EcdFieldParser.GetField(line, 7)?.Trim(),
                EcdFieldParser.GetField(line, 3)?.Trim(),
                EcdFieldParser.GetField(line, 4)?.Trim()));
        }

        return accounts;
    }

    public static HashSet<string> BuildCodeSet(IReadOnlyList<EcdAccountEntry> accounts) =>
        accounts.Select(a => a.CodConta).ToHashSet(StringComparer.Ordinal);

    public static bool IsValidNatureza(string? natureza) =>
        !string.IsNullOrWhiteSpace(natureza) && ValidNaturezas.Contains(natureza);
}
