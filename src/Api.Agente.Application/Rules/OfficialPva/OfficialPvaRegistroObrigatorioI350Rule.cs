using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaRegistroObrigatorioI350Rule : IValidationRule
{
    public const string RuleId = "REGRA_REGISTRO_OBRIGATORIO_I350";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaRegistroObrigatorioI350Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
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
        if (closing.Apuracoes.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Nenhum registro I350 informado.",
            });
        }

        var journal = EcdJournalIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var apuracao in closing.Apuracoes)
        {
            if (journal.HasEncerramentoLancamentoOn(apuracao.DtRes))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I350",
                LineNumber = apuracao.LineNumber,
                Message =
                    $"Na linha {apuracao.LineNumber}, DT_RES {apuracao.DtRes:dd/MM/yyyy}: "
                    + "não há lançamento I200 com IND_LCTO=E na mesma data.",
            });
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Para cada I350 informado existe lançamento de encerramento (I200, IND_LCTO=E).",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "Com I350 (DT_RES) informado, deve existir lançamento de encerramento "
                    + "no I200 (IND_LCTO=E) na mesma data, para escriturações G ou R.",
                Evidence = evidence,
            });
    }
}
