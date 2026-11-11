using Api.Agente.Domain.Ecd;

namespace Api.Agente.Application.Ecd;

public sealed record EcdJ005Header(
    int LineNumber,
    DateOnly DtIni,
    DateOnly DtFin,
    string? IdDem,
    string? CabDem);

public sealed record EcdDemonstracaoAglutinacaoLine(
    int LineNumber,
    string CodAgl,
    string IndCodAgl,
    int Nivel,
    string? CodAglSup,
    string? IndGrpBal,
    decimal VlIni,
    string? IndDcIni,
    decimal VlFin,
    string? IndDcFin);

public sealed class EcdDemonstracoesBloco
{
    public EcdJ005Header Header { get; init; } = null!;

    public IReadOnlyList<EcdDemonstracaoAglutinacaoLine> BalancoPatrimonial { get; init; } =
        Array.Empty<EcdDemonstracaoAglutinacaoLine>();

    public IReadOnlyList<EcdDemonstracaoAglutinacaoLine> DemonstracaoResultado { get; init; } =
        Array.Empty<EcdDemonstracaoAglutinacaoLine>();

    public int J210LineCount { get; init; }

    public int J100LineCount { get; init; }

    public int J150LineCount { get; init; }

    public bool HasJ100 => J100LineCount > 0;

    public bool HasJ150 => J150LineCount > 0;

    public bool HasJ210 => J210LineCount > 0;
}

public sealed class EcdDemonstracoesIndex
{
    private readonly List<EcdDemonstracoesBloco> _blocos;

    private EcdDemonstracoesIndex(List<EcdDemonstracoesBloco> blocos) => _blocos = blocos;

    public IReadOnlyList<EcdDemonstracoesBloco> Blocos => _blocos;

    public bool HasBlocoJ => _blocos.Count > 0;

    public static EcdDemonstracoesIndex Build(IReadOnlyList<EcdLine> lines)
    {
        var blocos = new List<EcdDemonstracoesBloco>();
        EcdJ005Header? current = null;
        var j100 = new List<EcdDemonstracaoAglutinacaoLine>();
        var j150 = new List<EcdDemonstracaoAglutinacaoLine>();
        var j210Count = 0;
        var j100Count = 0;
        var j150Count = 0;

        void Flush()
        {
            if (current is null)
            {
                return;
            }

            blocos.Add(new EcdDemonstracoesBloco
            {
                Header = current,
                BalancoPatrimonial = j100,
                DemonstracaoResultado = j150,
                J210LineCount = j210Count,
                J100LineCount = j100Count,
                J150LineCount = j150Count,
            });
            current = null;
            j100 = new List<EcdDemonstracaoAglutinacaoLine>();
            j150 = new List<EcdDemonstracaoAglutinacaoLine>();
            j210Count = 0;
            j100Count = 0;
            j150Count = 0;
        }

        foreach (var line in lines)
        {
            if (line.RecordType is "J001" or "J990" or "9001")
            {
                Flush();
                continue;
            }

            if (line.RecordType == "J005")
            {
                Flush();
                if (TryParseJ005(line, out current))
                {
                    continue;
                }

                current = null;
                continue;
            }

            if (current is null)
            {
                continue;
            }

            if (line.RecordType == "J100")
            {
                j100Count++;
                if (TryParseJ100(line, out var j100Line))
                {
                    j100.Add(j100Line);
                }

                continue;
            }

            if (line.RecordType == "J150")
            {
                j150Count++;
                if (TryParseJ150(line, out var j150Line))
                {
                    j150.Add(j150Line);
                }

                continue;
            }

            if (line.RecordType == "J210")
            {
                j210Count++;
            }
        }

        Flush();
        return new EcdDemonstracoesIndex(blocos);
    }

    private static bool TryParseJ005(EcdLine line, out EcdJ005Header header)
    {
        header = null!;
        if (!EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(line, 2), out var dtIni) ||
            !EcdFieldParser.TryParseSpedDate(EcdFieldParser.GetField(line, 3), out var dtFin))
        {
            return false;
        }

        header = new EcdJ005Header(
            line.LineNumber,
            dtIni,
            dtFin,
            EcdFieldParser.GetField(line, 4)?.Trim(),
            EcdFieldParser.GetField(line, 5)?.Trim());
        return true;
    }

    private static bool TryParseJ100(EcdLine line, out EcdDemonstracaoAglutinacaoLine result) =>
        TryParseAglutinacaoCore(
            line,
            codAglField: 2,
            indField: 3,
            nivelField: 4,
            supField: 5,
            grpBalField: 6,
            vlIniField: 8,
            indDcIniField: 9,
            vlFinField: 10,
            indDcFinField: 11,
            out result);

    private static bool TryParseJ150(EcdLine line, out EcdDemonstracaoAglutinacaoLine result) =>
        TryParseAglutinacaoCore(
            line,
            codAglField: 3,
            indField: 4,
            nivelField: 5,
            supField: 6,
            grpBalField: null,
            vlIniField: 8,
            indDcIniField: 9,
            vlFinField: 10,
            indDcFinField: 11,
            out result);

    private static bool TryParseAglutinacaoCore(
        EcdLine line,
        int codAglField,
        int indField,
        int nivelField,
        int supField,
        int? grpBalField,
        int vlIniField,
        int indDcIniField,
        int vlFinField,
        int indDcFinField,
        out EcdDemonstracaoAglutinacaoLine result)
    {
        result = null!;
        var codAgl = EcdFieldParser.GetField(line, codAglField)?.Trim();
        if (string.IsNullOrWhiteSpace(codAgl))
        {
            return false;
        }

        var ind = EcdFieldParser.GetField(line, indField)?.Trim().ToUpperInvariant() ?? string.Empty;
        if (ind is not ("T" or "D"))
        {
            return false;
        }

        var nivelText = EcdFieldParser.GetField(line, nivelField)?.Trim();
        if (!int.TryParse(nivelText, out var nivel))
        {
            return false;
        }

        if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, vlIniField), out var vlIni) ||
            !EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(line, vlFinField), out var vlFin))
        {
            return false;
        }

        result = new EcdDemonstracaoAglutinacaoLine(
            line.LineNumber,
            codAgl,
            ind,
            nivel,
            EcdFieldParser.GetField(line, supField)?.Trim(),
            grpBalField is null
                ? null
                : EcdFieldParser.GetField(line, grpBalField.Value)?.Trim().ToUpperInvariant(),
            vlIni,
            EcdFieldParser.GetField(line, indDcIniField)?.Trim().ToUpperInvariant(),
            vlFin,
            EcdFieldParser.GetField(line, indDcFinField)?.Trim().ToUpperInvariant());
        return true;
    }
}

public static class EcdDemonstracaoSaldo
{
    private const decimal Tolerance = 0.001m;

    public static decimal SignedIni(EcdDemonstracaoAglutinacaoLine line) =>
        EcdSignedAmount.FromDebitCredit(line.VlIni, line.IndDcIni);

    public static decimal SignedFin(EcdDemonstracaoAglutinacaoLine line) =>
        EcdSignedAmount.FromDebitCredit(line.VlFin, line.IndDcFin);

    public static bool AmountsEqual(decimal expected, decimal actual) =>
        Math.Abs(expected - actual) <= Tolerance;
}
