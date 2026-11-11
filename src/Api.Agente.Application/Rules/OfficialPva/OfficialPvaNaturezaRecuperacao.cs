namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaNaturezaRecuperacao
{
    /// <summary>
    /// Manual p. 139: I050 01–03 ⇒ C050 01–03; I050 04 ⇒ C050 04 (mesmo COD_CTA no C155).
    /// </summary>
    public static bool NaturezaI155DivergeRecuperacao(string? natI050, string? natC050)
    {
        if (string.IsNullOrWhiteSpace(natI050) || string.IsNullOrWhiteSpace(natC050))
        {
            return false;
        }

        if (natI050 is "01" or "02" or "03")
        {
            return natC050 is not ("01" or "02" or "03") ||
                   !string.Equals(natI050, natC050, StringComparison.Ordinal);
        }

        if (natI050 == "04")
        {
            return natC050 != "04";
        }

        return false;
    }

    public static bool NaturezaC155DivergeI050(string? natI050, string? natC050) =>
        NaturezaI155DivergeRecuperacao(natI050, natC050);
}
