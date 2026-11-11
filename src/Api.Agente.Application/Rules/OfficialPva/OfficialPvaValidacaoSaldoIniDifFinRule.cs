using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaValidacaoSaldoIniDifFinRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_SALDO_INI_DIF_FIN";
    public const string RuleVersion = "1.0.0";

    private const decimal Tolerance = 0.001m;

    public string ImplementationKey => nameof(OfficialPvaValidacaoSaldoIniDifFinRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(SkipMf());
        }

        var index = EcdPeriodicBalanceIndex.Build(context.Lines);
        var ordered = index.Periods.OrderBy(p => p.DtIni).ToList();
        if (ordered.Count < 2)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Menos de dois períodos I150; regra não aplicável.",
            });
        }

        var evidence = new List<RuleEvidence>();
        for (var i = 1; i < ordered.Count; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];
            foreach (var balance in index.GetBalancesForPeriod(current))
            {
                var prior = index.TryGetBalance(previous, balance.Account);
                if (prior is null)
                {
                    continue;
                }

                var expectedIni = EcdSignedAmount.FromDebitCredit(
                    prior.SaldoFinal,
                    prior.IndicadorSaldoFinal);
                var actualIni = EcdSignedAmount.FromDebitCredit(
                    balance.SaldoInicial,
                    balance.IndicadorSaldoInicial);

                if (Math.Abs(expectedIni - actualIni) <= Tolerance)
                {
                    continue;
                }

                evidence.Add(new RuleEvidence
                {
                    RecordType = "I155",
                    LineNumber = balance.LineNumber,
                    ExpectedValue = expectedIni.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    FoundValue = actualIni.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    Message =
                        $"Na linha {balance.LineNumber}, conta {balance.Account.CodConta}: "
                        + "VL_SLD_INI deve igualar VL_SLD_FIN do período I150 anterior (com D/C).",
                });
            }
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "VL_SLD_INI consistente com VL_SLD_FIN do período anterior.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "A partir do 2º período I150, VL_SLD_INI deve igualar VL_SLD_FIN do mês anterior.",
                Evidence = evidence,
            });
    }

    private static RuleEvaluationResult SkipMf() => new()
    {
        RuleId = RuleId,
        RuleVersion = RuleVersion,
        Outcome = RuleOutcome.Validated,
        Message = "IDENT_MF=S; usar REGRA_VALIDACAO_SALDO_INI_DIF_FIN_MF.",
    };
}
