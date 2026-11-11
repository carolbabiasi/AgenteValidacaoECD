using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaEscrituracao
{
    public static string? GetIndEsc(IReadOnlyList<EcdLine> lines)
    {
        var i010 = lines.FirstOrDefault(l => l.RecordType == "I010");
        return i010 is null ? null : EcdFieldParser.GetField(i010, 2)?.Trim().ToUpperInvariant();
    }

    public static bool RuleAppliesForFullDiary(string? indEsc) =>
        indEsc is not null and not "A";

    public static bool RuleAppliesForAuxiliaryDiary(string? indEsc) =>
        string.Equals(indEsc, "A", StringComparison.Ordinal);

    /// <summary>0000.IDENT_MF (campo 19) = S indica ECD em moeda funcional.</summary>
    public static bool UsesMoedaFuncional(IReadOnlyList<EcdLine> lines)
    {
        var opening = lines.FirstOrDefault(l => l.RecordType == "0000");
        if (opening is null)
        {
            return false;
        }

        var ident = EcdFieldParser.GetField(opening, 19)?.Trim();
        return string.Equals(ident, "S", StringComparison.OrdinalIgnoreCase);
    }
}
