using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaValidacaoContaResultadoRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_CONTA_RESULTADO";
    public const string RuleVersion = "1.0.0";

    private const decimal Tolerance = 0.001m;

    public string ImplementationKey => nameof(OfficialPvaValidacaoContaResultadoRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(ValidatedSkippedMoedaFuncional());
        }

        var closing = EcdClosingIndex.Build(context.Lines);
        if (closing.Apuracoes.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Nenhum registro I350 informado.",
            });
        }

        var balances = EcdPeriodicBalanceIndex.Build(context.Lines);
        var chart = EcdChartOfAccountsIndex.Parse(context.Lines);
        var resultAccounts = BuildResultAccountCodes(chart);
        var evidence = new List<RuleEvidence>();

        foreach (var apuracao in closing.Apuracoes)
        {
            var period = balances.FindPeriodContaining(apuracao.DtRes);
            if (period is null)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I350",
                    LineNumber = apuracao.LineNumber,
                    Message =
                        $"Na linha {apuracao.LineNumber}, DT_RES {apuracao.DtRes:dd/MM/yyyy}: "
                        + "não há período I150 que contenha a data de encerramento.",
                });
                continue;
            }

            var signedSum = 0m;
            var contributed = false;
            foreach (var balance in balances.GetBalancesForPeriod(period))
            {
                if (!resultAccounts.Contains(balance.Account.CodConta))
                {
                    continue;
                }

                contributed = true;
                signedSum += EcdSignedAmount.FromDebitCredit(
                    balance.SaldoFinal,
                    balance.IndicadorSaldoFinal);
            }

            if (!contributed)
            {
                continue;
            }

            if (Math.Abs(signedSum) <= Tolerance)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I350",
                LineNumber = apuracao.LineNumber,
                ExpectedValue = "0,00",
                FoundValue = signedSum.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na data de encerramento {apuracao.DtRes:dd/MM/yyyy}: a soma dos VL_SLD_FIN "
                    + "(com D/C) das contas de resultado (I050 COD_NAT=04) no período I150 "
                    + $"deve ser zero ({EcdDisplayText.FormatMoney(signedSum)}).",
            });
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "Na data de encerramento (I350), a soma dos saldos finais das contas de resultado (I155) é zero.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "Na data de encerramento, a soma dos VL_SLD_FIN das contas de resultado (I155) deve ser igual a zero.",
                Evidence = evidence,
            });
    }

    private static HashSet<string> BuildResultAccountCodes(IReadOnlyList<EcdAccountEntry> chart) =>
        chart
            .Where(a => string.Equals(a.Natureza, "04", StringComparison.Ordinal))
            .Select(a => a.CodConta)
            .ToHashSet(StringComparer.Ordinal);

    private static RuleEvaluationResult ValidatedSkippedMoedaFuncional() => new()
    {
        RuleId = RuleId,
        RuleVersion = RuleVersion,
        Outcome = RuleOutcome.Validated,
        Message = "IDENT_MF=S; usar REGRA_VALIDACAO_CONTA_RESULTADO_MF.",
    };
}
