using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaDuplicidadeContaSaldoPeriodicoRule : IValidationRule
{
    public const string RuleId = "REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaDuplicidadeContaSaldoPeriodicoRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var index = EcdPeriodicBalanceIndex.Build(context.Lines);
        var byKey = new Dictionary<(int PeriodLine, string CodConta, string CodCcus), List<EcdPeriodicBalanceLine>>();

        foreach (var balance in index.Balances)
        {
            var key = (balance.Period.HeaderLineNumber, balance.Account.CodConta, balance.Account.CodCcus);
            if (!byKey.TryGetValue(key, out var list))
            {
                list = [];
                byKey[key] = list;
            }

            list.Add(balance);
        }

        var evidence = new List<RuleEvidence>();
        foreach (var (key, lines) in byKey.Where(kv => kv.Value.Count > 1))
        {
            foreach (var balance in lines)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I155",
                    LineNumber = balance.LineNumber,
                    FoundValue = $"{key.CodConta}+{key.CodCcus}",
                    Message =
                        $"Na linha {balance.LineNumber}, período I150 linha {key.PeriodLine}: "
                        + "conta e centro de custos duplicados (COD_CTA+COD_CCUS).",
                });
            }
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Não há duplicidade de conta/CC por período em I155.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "Por período (I150), COD_CTA + COD_CCUS devem ser únicos no I155.",
                Evidence = evidence,
            });
    }
}
