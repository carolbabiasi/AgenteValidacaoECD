using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaValidacaoSaldoContaRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_SALDO_CONTA";
    public const string RuleVersion = "1.0.0";

    private const decimal Tolerance = 0.001m;

    public string ImplementationKey => nameof(OfficialPvaValidacaoSaldoContaRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(ValidatedSkippedMoedaFuncional());
        }

        var indEsc = OfficialPvaEscrituracao.GetIndEsc(context.Lines);
        if (!OfficialPvaEscrituracao.RuleAppliesForFullDiary(indEsc))
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Regra aplica-se apenas a escriturações G ou R.",
            });
        }

        var closing = EcdClosingIndex.Build(context.Lines);
        if (closing.Balances.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Nenhum registro I355 para validar.",
            });
        }

        var journal = EcdJournalIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var balance in closing.Balances)
        {
            var expected = EcdSignedAmount.FromEncerramentoSaldoConta(balance.Valor, balance.IndicadorDc);
            var actual = journal.SumEncerramentoLancamentos(balance.Apuration.DtRes, balance.Account);

            if (Math.Abs(expected - actual) <= Tolerance)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I355",
                LineNumber = balance.LineNumber,
                ExpectedValue = expected.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue = actual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"Na linha {balance.LineNumber}, conta {balance.Account.CodConta}, DT_RES "
                    + $"{balance.Apuration.DtRes:dd/MM/yyyy}: soma dos lançamentos tipo E "
                    + "não confere com VL_CTA (IND_DC invertido).",
            });
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "Saldos I355 consistentes com a soma dos lançamentos de encerramento (I200/E, I250).",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "A soma dos lançamentos tipo E por data e conta deve igualar VL_CTA (I355), "
                    + "com IND_DC invertido, para escriturações G ou R.",
                Evidence = evidence,
            });
    }

    private static RuleEvaluationResult ValidatedSkippedMoedaFuncional() => new()
    {
        RuleId = RuleId,
        RuleVersion = RuleVersion,
        Outcome = RuleOutcome.Validated,
        Message = "IDENT_MF=S; usar REGRA_VALIDACAO_SALDO_CONTA_MF.",
    };
}
