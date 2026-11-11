using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

/// <summary>
/// Manual ECD Leiaute 9 — REGRA_OBRIG_NAO_MF_I200: VL_LCTO (campo 04) deve ser diferente de zero
/// quando o arquivo não usa moeda funcional (0000.IDENT_MF ≠ S).
/// </summary>
public sealed class OfficialPvaObrigNaoMfI200Rule : IValidationRule
{
    public const string RuleId = "REGRA_OBRIG_NAO_MF_I200";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaObrigNaoMfI200Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Arquivo com moeda funcional (IDENT_MF=S); validação de VL_LCTO_MF fica para REGRA_OBRIG_MF_I200.",
            });
        }

        var journal = EcdJournalIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var header in journal.Headers)
        {
            var i200 = context.Lines.FirstOrDefault(l => l.LineNumber == header.LineNumber);
            if (i200 is null)
            {
                continue;
            }

            if (!EcdFieldParser.TryParseSpedDecimal(EcdFieldParser.GetField(i200, 4), out var vlLcto))
            {
                continue;
            }

            if (vlLcto != 0)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I200",
                LineNumber = header.LineNumber,
                ExpectedValue = "diferente de zero",
                FoundValue = "0,00",
                Message =
                    $"Na linha {header.LineNumber}, o valor do lançamento VL_LCTO (campo 04) deve ser diferente de zero.",
            });
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Todos os lançamentos I200 possuem VL_LCTO diferente de zero.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message =
                "O valor do lançamento VL_LCTO (campo 04 do registro I200) deve ser diferente de zero.",
            Evidence = evidence,
        });
    }
}
