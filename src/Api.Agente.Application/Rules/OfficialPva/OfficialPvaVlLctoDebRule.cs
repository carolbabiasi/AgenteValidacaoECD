using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaVlLctoDebRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_VL_LCTO_DEB";
    public const string RuleVersion = "1.0.0";
    public const string OfficialCode = RuleId;

    public string ImplementationKey => nameof(OfficialPvaVlLctoDebRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = OfficialPvaVlLctoValidation.CollectMismatches(context.Lines, validateDebits: true);

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "Soma dos débitos (I250) igual ao VL_LCTO (I200) para escrituração não auxiliar (IND_ESC ≠ A).",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "A soma dos débitos das partidas (I250) deve ser igual ao valor do lançamento (VL_LCTO) no registro I200.",
            Evidence = evidence,
        });
    }
}
