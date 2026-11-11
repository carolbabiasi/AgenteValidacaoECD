using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaVlLctoCredRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_VL_LCTO_CRED";
    public const string RuleVersion = "1.0.0";
    public const string OfficialCode = RuleId;

    public string ImplementationKey => nameof(OfficialPvaVlLctoCredRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = OfficialPvaVlLctoValidation.CollectMismatches(context.Lines, validateDebits: false);

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "Soma dos créditos (I250) igual ao VL_LCTO (I200) para escrituração não auxiliar (IND_ESC ≠ A).",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "A soma dos créditos das partidas (I250) deve ser igual ao valor do lançamento (VL_LCTO) no registro I200.",
            Evidence = evidence,
        });
    }
}
