using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaVlLctoValidation
{
    public static List<RuleEvidence> CollectMismatches(
        IReadOnlyList<EcdLine> lines,
        bool validateDebits)
    {
        if (!OfficialPvaEscrituracao.RuleAppliesForFullDiary(OfficialPvaEscrituracao.GetIndEsc(lines)))
        {
            return [];
        }

        var lineByNumber = lines.ToDictionary(l => l.LineNumber);
        var journal = EcdJournalIndex.Build(lines);
        var evidence = new List<RuleEvidence>();

        foreach (var header in journal.Headers)
        {
            if (!lineByNumber.TryGetValue(header.LineNumber, out var i200))
            {
                continue;
            }

            if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(i200, 4), out var vlLcto))
            {
                continue;
            }

            var (debits, credits) = journal.SumParts(header);
            var actual = validateDebits ? debits : credits;
            if (actual == vlLcto)
            {
                continue;
            }

            var side = validateDebits ? "débitos" : "créditos";
            evidence.Add(new RuleEvidence
            {
                RecordType = "I200",
                LineNumber = header.LineNumber,
                ExpectedValue = vlLcto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue = actual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na linha {header.LineNumber}, a soma dos {side} das partidas (I250) "
                    + $"({EcdDisplayText.FormatMoney(actual)}) difere do valor do lançamento VL_LCTO "
                    + $"({EcdDisplayText.FormatMoney(vlLcto)}).",
            });
        }

        return evidence;
    }

    public static List<RuleEvidence> CollectAuxiliaryMismatches(IReadOnlyList<EcdLine> lines)
    {
        if (!OfficialPvaEscrituracao.RuleAppliesForAuxiliaryDiary(OfficialPvaEscrituracao.GetIndEsc(lines)))
        {
            return [];
        }

        var lineByNumber = lines.ToDictionary(l => l.LineNumber);
        var journal = EcdJournalIndex.Build(lines);
        var evidence = new List<RuleEvidence>();

        foreach (var header in journal.Headers)
        {
            if (!lineByNumber.TryGetValue(header.LineNumber, out var i200))
            {
                continue;
            }

            if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(i200, 4), out var vlLcto))
            {
                continue;
            }

            var (debits, credits) = journal.SumParts(header);
            if (debits == vlLcto || credits == vlLcto)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I200",
                LineNumber = header.LineNumber,
                ExpectedValue = vlLcto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue =
                    $"D={debits.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)};"
                    + $"C={credits.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}",
                Message =
                    $"Na linha {header.LineNumber}, escrituração auxiliar (IND_ESC=A): nem a soma dos débitos "
                    + $"({EcdDisplayText.FormatMoney(debits)}) nem dos créditos ({EcdDisplayText.FormatMoney(credits)}) "
                    + $"iguala o VL_LCTO ({EcdDisplayText.FormatMoney(vlLcto)}).",
            });
        }

        return evidence;
    }
}
