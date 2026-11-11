using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

internal static class OfficialPvaValidacaoSomaSaldoRule
{
    public static List<RuleEvidence> CollectMismatches(IReadOnlyList<EcdLine> lines, bool saldoInicial)
    {
        var indEsc = OfficialPvaEscrituracao.GetIndEsc(lines);
        if (indEsc is not ("G" or "R") || OfficialPvaEscrituracao.UsesMoedaFuncional(lines))
        {
            return [];
        }

        var index = EcdPeriodicBalanceIndex.Build(lines);
        var evidence = new List<RuleEvidence>();

        foreach (var period in index.Periods)
        {
            decimal signedSum = 0;
            foreach (var balance in index.GetBalancesForPeriod(period))
            {
                signedSum += saldoInicial
                    ? EcdSignedAmount.FromDebitCredit(balance.SaldoInicial, balance.IndicadorSaldoInicial)
                    : EcdSignedAmount.FromDebitCredit(balance.SaldoFinal, balance.IndicadorSaldoFinal);
            }

            if (Math.Abs(signedSum) < 0.001m)
            {
                continue;
            }

            var campo = saldoInicial ? "VL_SLD_INI" : "VL_SLD_FIN";
            evidence.Add(new RuleEvidence
            {
                RecordType = "I150",
                LineNumber = period.HeaderLineNumber,
                ExpectedValue = "0,00",
                FoundValue = signedSum.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"No período da linha {period.HeaderLineNumber}: soma dos {campo} (com indicadores D/C) "
                    + $"deve ser zero em escrituração G/R ({EcdDisplayText.FormatMoney(signedSum)}).",
            });
        }

        return evidence;
    }
}

public sealed class OfficialPvaValidacaoSomaSaldoInicialRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_SOMA_SALDO_INICIAL";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidacaoSomaSaldoInicialRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = OfficialPvaValidacaoSomaSaldoRule.CollectMismatches(context.Lines, saldoInicial: true);
        return Task.FromResult(BuildResult(evidence, RuleId, RuleVersion));
    }

    private static RuleEvaluationResult BuildResult(List<RuleEvidence> evidence, string ruleId, string version) =>
        evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = ruleId,
                RuleVersion = version,
                Outcome = RuleOutcome.Validated,
                Message = "Soma dos saldos iniciais do período é zero (G/R).",
            }
            : new RuleEvaluationResult
            {
                RuleId = ruleId,
                RuleVersion = version,
                Outcome = RuleOutcome.Error,
                Message = "A soma dos VL_SLD_INI do período (I155), com D/C, deve ser zero.",
                Evidence = evidence,
            };
}

public sealed class OfficialPvaValidacaoSomaSaldoFinalRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_SOMA_SALDO_FINAL";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidacaoSomaSaldoFinalRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = OfficialPvaValidacaoSomaSaldoRule.CollectMismatches(context.Lines, saldoInicial: false);
        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Soma dos saldos finais do período é zero (G/R).",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message = "A soma dos VL_SLD_FIN do período (I155), com D/C, deve ser zero.",
                Evidence = evidence,
            });
    }
}
