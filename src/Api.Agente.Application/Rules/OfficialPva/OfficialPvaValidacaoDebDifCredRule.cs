using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

/// <summary>
/// Manual — REGRA_VALIDACAO_DEB_DIF_CRED: por período I150, Σ VL_DEB = Σ VL_CRED (I155) em G/R.
/// </summary>
public sealed class OfficialPvaValidacaoDebDifCredRule : IValidationRule
{
    public const string RuleId = "REGRA_VALIDACAO_DEB_DIF_CRED";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaValidacaoDebDifCredRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var indEsc = OfficialPvaEscrituracao.GetIndEsc(context.Lines);
        if (indEsc is not ("G" or "R") || OfficialPvaEscrituracao.UsesMoedaFuncional(context.Lines))
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "REGRA_VALIDACAO_DEB_DIF_CRED aplica-se a IND_ESC G ou R sem moeda funcional.",
            });
        }

        var index = EcdPeriodicBalanceIndex.Build(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var period in index.Periods)
        {
            var lines = index.GetBalancesForPeriod(period).ToList();
            var sumDeb = lines.Sum(l => l.TotalDebitos);
            var sumCred = lines.Sum(l => l.TotalCreditos);

            if (sumDeb == sumCred)
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I150",
                LineNumber = period.HeaderLineNumber,
                ExpectedValue = sumCred.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                FoundValue = sumDeb.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Message =
                    $"No período da linha {period.HeaderLineNumber} ({period.DtIni:dd/MM/yyyy}–{period.DtFin:dd/MM/yyyy}): "
                    + $"soma de VL_DEB ({EcdDisplayText.FormatMoney(sumDeb)}) difere da soma de VL_CRED "
                    + $"({EcdDisplayText.FormatMoney(sumCred)}).",
            });
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Totais de débitos e créditos do período (I155) estão balanceados.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message = "Por período I150, a soma dos VL_DEB deve igualar a soma dos VL_CRED nos I155.",
                Evidence = evidence,
            });
    }
}
