using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaValidacaoSaldoFinalRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_SALDO_FINAL";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidacaoSaldoFinalRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(ValidatedSkippedMoedaFuncional());
        }

        var balances = EcdPeriodicBalanceIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var balance in balances.Balances)
        {
            var expected = OfficialPvaI155SignedBalance.ComputeExpectedFinal(
                balance.SaldoInicial,
                balance.IndicadorSaldoInicial,
                balance.TotalDebitos,
                balance.TotalCreditos);

            if (OfficialPvaI155SignedBalance.MatchesDeclared(
                    expected,
                    balance.SaldoFinal,
                    balance.IndicadorSaldoFinal))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I155",
                LineNumber = balance.LineNumber,
                ExpectedValue = expected.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue = EcdSignedAmount
                    .FromDebitCredit(balance.SaldoFinal, balance.IndicadorSaldoFinal)
                    .ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na linha {balance.LineNumber}, conta {balance.Account.CodConta}: VL_SLD_FIN (com IND_DC_FIN) "
                    + "não confere com VL_SLD_INI + VL_DEB − VL_CRED.",
            });
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Saldos finais I155 consistentes com saldo inicial e movimentação do período.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "VL_SLD_FIN deve igualar VL_SLD_INI + VL_DEB − VL_CRED, respeitando IND_DC_INI e IND_DC_FIN.",
                Evidence = evidence,
            });
    }

    private static RuleEvaluationResult ValidatedSkippedMoedaFuncional() => new()
    {
        RuleId = RuleId,
        RuleVersion = RuleVersion,
        Outcome = RuleOutcome.Validated,
        Message = "IDENT_MF=S; usar REGRA_VALIDACAO_SALDO_FINAL_MF.",
    };
}
