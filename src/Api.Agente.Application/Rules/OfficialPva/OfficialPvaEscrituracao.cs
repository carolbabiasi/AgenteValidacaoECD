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
}
