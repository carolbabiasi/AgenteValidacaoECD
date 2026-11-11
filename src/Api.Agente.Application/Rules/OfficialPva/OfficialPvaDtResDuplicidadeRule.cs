using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaDtResDuplicidadeRule : IValidationRule
{
    public const string RuleId = "REGRA_DT_RES_DUPLICIDADE";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaDtResDuplicidadeRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var closing = EcdClosingIndex.Build(context.Lines);
        var byDate = new Dictionary<DateOnly, List<EcdClosingApuration>>();

        foreach (var apuracao in closing.Apuracoes)
        {
            if (!byDate.TryGetValue(apuracao.DtRes, out var list))
            {
                list = [];
                byDate[apuracao.DtRes] = list;
            }

            list.Add(apuracao);
        }

        var evidence = new List<RuleEvidence>();
        foreach (var (dtRes, apuracoes) in byDate.Where(kv => kv.Value.Count > 1))
        {
            foreach (var apuracao in apuracoes)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I350",
                    LineNumber = apuracao.LineNumber,
                    FoundValue = dtRes.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    Message =
                        $"Na linha {apuracao.LineNumber}, DT_RES {dtRes:dd/MM/yyyy} "
                        + "está duplicada no arquivo (chave do registro I350).",
                });
            }
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Não há DT_RES duplicada em registros I350.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message = "A data de apuração DT_RES (campo 02 do I350) deve ser única no arquivo.",
                Evidence = evidence,
            });
    }
}
