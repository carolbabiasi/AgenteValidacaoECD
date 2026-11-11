using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

/// <summary>
/// Manual ECD Leiaute 9 — REGRA_LCTO_4_FORMULA: IND_LCTO = N e menos de 2 partidas D e menos de 2 partidas C no I250.
/// </summary>
public sealed class OfficialPvaLcto4FormulaRule : IValidationRule
{
    public const string RuleId = "REGRA_LCTO_4_FORMULA";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaLcto4FormulaRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var journal = EcdJournalIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var header in journal.Headers)
        {
            if (!string.Equals(header.IndicadorLancamento, "N", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = journal.GetParts(header).ToList();
            var debitCount = parts.Count(p => p.IndicadorDc == "D");
            var creditCount = parts.Count(p => p.IndicadorDc == "C");

            if (debitCount >= 2 || creditCount >= 2)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I200",
                LineNumber = header.LineNumber,
                FoundValue = $"D={debitCount};C={creditCount}",
                Message =
                    $"Na linha {header.LineNumber}, lançamento normal (IND_LCTO=N): "
                    + $"há menos de 2 partidas a débito ({debitCount}) e menos de 2 a crédito ({creditCount}) no I250.",
            });
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Lançamentos normais (IND_LCTO=N) atendem REGRA_LCTO_4_FORMULA.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Alert,
            Message =
                "Lançamento normal (IND_LCTO=N) com menos de 2 partidas a débito e menos de 2 a crédito (I250).",
            Evidence = evidence,
        });
    }
}
