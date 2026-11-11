using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaVlLctoEscAuxiliarRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaVlLctoEscAuxiliarRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var evidence = OfficialPvaVlLctoValidation.CollectAuxiliaryMismatches(context.Lines);

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "VL_LCTO coerente com soma de débitos ou de créditos (I250) na escrituração auxiliar (IND_ESC=A).",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Alert,
            Message =
                "Na escrituração auxiliar (IND_ESC=A), a soma dos débitos ou dos créditos (I250) deve igualar o VL_LCTO (I200).",
            Evidence = evidence,
        });
    }
}
