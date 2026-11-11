using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaSomaNiveisDreRule : IValidationRule
{
    public const string RuleId = "REGRA_SOMA_NIVEIS_DRE";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaSomaNiveisDreRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var demos = EcdDemonstracoesIndex.Build(context.Lines);
        var evidence = new List<Domain.ValueObjects.RuleEvidence>();
        foreach (var bloco in demos.Blocos)
        {
            OfficialPvaDemonstracaoBalancoHelpers.ValidateSomaTotalizadores(
                bloco.DemonstracaoResultado,
                saldoInicial: false,
                recordType: "J150",
                evidence);
        }

        return Task.FromResult(OfficialPvaDemonstracaoRuleHelpers.Build(
            evidence,
            RuleId,
            RuleVersion,
            "Totalizadores J150 conferem com a soma dos filhos (VL_CTA_FIN).",
            "VL_CTA_FIN do totalizador J150 difere da soma dos filhos."));
    }
}
