using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed record EcdArquivoPeriodo(DateOnly DtIni, DateOnly DtFin);

public static class EcdArquivoIndex
{
    public static EcdArquivoPeriodo? TryGetPeriodo0000(IReadOnlyList<EcdLine> lines)
    {
        var opening = lines.FirstOrDefault(l => l.RecordType == "0000");
        if (opening is null)
        {
            return null;
        }

        if (!EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(opening, 3), out var dtIni) ||
            !EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(opening, 4), out var dtFin))
        {
            return null;
        }

        return new EcdArquivoPeriodo(dtIni, dtFin);
    }

    public static DateOnly? TryGetDtExSocial(IReadOnlyList<EcdLine> lines)
    {
        var i030 = lines.FirstOrDefault(l => l.RecordType == "I030");
        if (i030 is null)
        {
            return null;
        }

        var dnrc = EcdFieldParser.GetField(i030, 2)?.Trim();
        if (string.Equals(dnrc, "TERMO DE ABERTURA", StringComparison.OrdinalIgnoreCase))
        {
            return EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(i030, 12), out var dt)
                ? dt
                : null;
        }

        if (EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(i030, 12), out var legacyDt))
        {
            return legacyDt;
        }

        return null;
    }

    public static bool DataNoPeriodoArquivo(DateOnly data, EcdArquivoPeriodo periodo) =>
        data >= periodo.DtIni && data <= periodo.DtFin;

    public static string? TryGetIndMudancaPlanoContas(IReadOnlyList<EcdLine> lines)
    {
        var opening = lines.FirstOrDefault(l => l.RecordType == "0000");
        if (opening is null)
        {
            return null;
        }

        var ind = EcdFieldParser.GetField(opening, 22)?.Trim();
        return string.IsNullOrEmpty(ind) ? "0" : ind;
    }

    public static IEnumerable<(int Year, int Month)> EnumerateMesesNoArquivo(EcdArquivoPeriodo periodo)
    {
        var cursor = new DateOnly(periodo.DtIni.Year, periodo.DtIni.Month, 1);
        var end = new DateOnly(periodo.DtFin.Year, periodo.DtFin.Month, 1);
        while (cursor <= end)
        {
            yield return (cursor.Year, cursor.Month);
            cursor = cursor.AddMonths(1);
        }
    }

    public static bool MesmoMesAno(DateOnly a, DateOnly b) =>
        a.Year == b.Year && a.Month == b.Month;
}
