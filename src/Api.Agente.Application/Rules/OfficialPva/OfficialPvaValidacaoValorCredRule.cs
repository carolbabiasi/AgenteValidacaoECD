using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaValidacaoValorCredRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_VALOR_CRED";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidacaoValorCredRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = OfficialPvaValidacaoValorPeriodoValidation.CollectMismatches(context.Lines, validateDebits: false);

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "Soma dos créditos (I250) por período e conta coincide com VL_CRED (I155) para G/R/A.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "A soma dos créditos dos lançamentos por período e conta deve igualar VL_CRED (campo 07) do I155.",
            Evidence = evidence,
        });
    }
}
