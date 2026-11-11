using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

/// <summary>
/// Manual — REGRA_CONTA_RESULTADO (registro I355): COD_CTA deve ser conta de resultado (I050.COD_NAT = 04).
/// </summary>
public sealed class OfficialPvaContaResultadoI355Rule : IValidationRule
{
    public const string RuleId = "REGRA_CONTA_RESULTADO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaContaResultadoI355Rule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var closing = EcdClosingIndex.Build(context.Lines);
        if (closing.Balances.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Nenhum registro I355 para validar.",
            });
        }

        var chart = EcdChartOfAccountsIndex.Parse(context.Lines);
        var byCode = chart.ToDictionary(a => a.CodConta, StringComparer.Ordinal);
        var evidence = new List<RuleEvidence>();

        foreach (var balance in closing.Balances)
        {
            if (!byCode.TryGetValue(balance.Account.CodConta, out var entry))
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I355",
                    LineNumber = balance.LineNumber,
                    FoundValue = balance.Account.CodConta,
                    Message =
                        $"Na linha {balance.LineNumber}, conta {balance.Account.CodConta} "
                        + "não existe no plano de contas (I050).",
                });
                continue;
            }

            if (string.Equals(entry.Natureza, "04", StringComparison.Ordinal))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I355",
                LineNumber = balance.LineNumber,
                FoundValue = entry.Natureza ?? "(vazio)",
                Message =
                    $"Na linha {balance.LineNumber}, conta {balance.Account.CodConta}: "
                    + "COD_NAT no I050 deve ser 04 (conta de resultado).",
            });
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Contas I355 são de resultado (COD_NAT=04 no I050).",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "I355.COD_CTA deve referenciar conta analítica de resultado (I050, COD_NAT = 04).",
                Evidence = evidence,
            });
    }
}
