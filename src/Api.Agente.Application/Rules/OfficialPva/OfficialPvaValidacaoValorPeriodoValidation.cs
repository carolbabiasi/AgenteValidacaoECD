using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaValidacaoValorPeriodoValidation
{
    public static bool RuleApplies(string? indEsc, IReadOnlyList<EcdLine> lines)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(lines))
        {
            return false;
        }

        return indEsc is "G" or "R" or "A";
    }

    public static List<RuleEvidence> CollectMismatches(IReadOnlyList<EcdLine> lines, bool validateDebits)
    {
        var indEsc = OfficialPvaEscrituracao.GetIndEsc(lines);
        if (!RuleApplies(indEsc, lines))
        {
            return [];
        }

        var balances = EcdPeriodicBalanceIndex.Build(lines);
        var journal = EcdJournalIndex.Build(lines);
        var evidence = new List<RuleEvidence>();

        foreach (var balance in balances.Balances)
        {
            var (debits, credits) = journal.SumPartsByAccount(balance.Period, balance.Account, balances);
            var expected = validateDebits ? balance.TotalDebitos : balance.TotalCreditos;
            var actual = validateDebits ? debits : credits;

            if (actual == expected)
            {
                continue;
            }

            var side = validateDebits ? "débitos" : "créditos";
            var campo = validateDebits ? "VL_DEB (06)" : "VL_CRED (07)";
            evidence.Add(new RuleEvidence
            {
                RecordType = "I155",
                LineNumber = balance.LineNumber,
                ExpectedValue = expected.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue = actual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na linha {balance.LineNumber}, conta {balance.Account.CodConta}: soma dos {side} "
                    + $"dos lançamentos no período ({EcdDisplayText.FormatMoney(actual)}) difere do "
                    + $"{campo} declarado ({EcdDisplayText.FormatMoney(expected)}).",
            });
        }

        return evidence;
    }
}
