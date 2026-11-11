using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaValidacaoValorDebRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_VALOR_DEB";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidacaoValorDebRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = OfficialPvaValidacaoValorPeriodoValidation.CollectMismatches(context.Lines, validateDebits: true);

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "Soma dos débitos (I250) por período e conta coincide com VL_DEB (I155) para G/R/A.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "A soma dos débitos dos lançamentos por período e conta deve igualar VL_DEB (campo 06) do I155.",
            Evidence = evidence,
        });
    }
}
