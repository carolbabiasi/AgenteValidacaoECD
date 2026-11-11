using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaEncerramentoExercicioRule : IValidationRule
{
    public const string RuleId = "REGRA_ENCERRAMENTO_EXERCICIO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaEncerramentoExercicioRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var periodo = EcdArquivoIndex.TryGetPeriodo0000(context.Lines);
        var dtExSocial = EcdArquivoIndex.TryGetDtExSocial(context.Lines);
        var indEsc = OfficialPvaEscrituracao.GetIndEsc(context.Lines);

        if (periodo is null || dtExSocial is null)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Sem período 0000 ou DT_EX_SOCIAL (I030) para avaliar encerramento do exercício.",
            });
        }

        if (indEsc is not ("G" or "R" or "B"))
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Regra aplica-se a escriturações G, R ou B.",
            });
        }

        if (!EcdArquivoIndex.DataNoPeriodoArquivo(dtExSocial.Value, periodo))
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "DT_EX_SOCIAL fora do intervalo do arquivo; condição da regra não aplicável.",
            });
        }

        var closing = EcdClosingIndex.Build(context.Lines);
        var hasMatchingI350 = closing.Apuracoes.Any(a => a.DtRes == dtExSocial.Value);

        if (hasMatchingI350)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message =
                    "Existe I350 com DT_RES igual à DT_EX_SOCIAL do I030.",
            });
        }

        var i030Line = context.Lines.First(l => l.RecordType == "I030");
        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "Com DT_EX_SOCIAL no período do arquivo (G/R/B), deve existir I350 com DT_RES = DT_EX_SOCIAL.",
            Evidence =
            [
                new RuleEvidence
                {
                    RecordType = "I030",
                    LineNumber = i030Line.LineNumber,
                    ExpectedValue = dtExSocial.Value.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    Message =
                        $"DT_EX_SOCIAL {dtExSocial.Value:dd/MM/yyyy}: falta registro I350 com a mesma DT_RES.",
                },
            ],
        });
    }
}
