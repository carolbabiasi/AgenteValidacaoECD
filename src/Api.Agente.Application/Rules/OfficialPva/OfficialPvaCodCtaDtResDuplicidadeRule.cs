using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.OfficialPva;

public sealed class OfficialPvaCodCtaDtResDuplicidadeRule : IValidationRule
{
    public const string RuleId = "REGRA_COD_CTA_DT_RES_DUPLICIDADE";
    public const string RuleVersion = "1.0.0";

    public string ImplementationKey => nameof(OfficialPvaCodCtaDtResDuplicidadeRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var closing = EcdClosingIndex.Build(context.Lines);
        var byKey = new Dictionary<(DateOnly DtRes, string CodConta, string CodCcus), List<EcdClosingAccountBalance>>();

        foreach (var balance in closing.Balances)
        {
            var key = (
                balance.Apuration.DtRes,
                balance.Account.CodConta,
                balance.Account.CodCcus);
            if (!byKey.TryGetValue(key, out var list))
            {
                list = [];
                byKey[key] = list;
            }

            list.Add(balance);
        }

        var evidence = new List<RuleEvidence>();
        foreach (var (key, balances) in byKey.Where(kv => kv.Value.Count > 1))
        {
            foreach (var balance in balances)
            {
                var ccus = string.IsNullOrEmpty(key.CodCcus) ? "(vazio)" : key.CodCcus;
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I355",
                    LineNumber = balance.LineNumber,
                    FoundValue = $"{key.CodConta}+{ccus}",
                    Message =
                        $"Na linha {balance.LineNumber}, DT_RES {key.DtRes:dd/MM/yyyy}: "
                        + $"conta {key.CodConta} e centro {ccus} duplicados (chave COD_CTA+COD_CCUS).",
                });
            }
        }

        return Task.FromResult(evidence.Count == 0
            ? new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Não há COD_CTA+COD_CCUS duplicados por DT_RES em I355.",
            }
            : new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Error,
                Message =
                    "Para a mesma DT_RES (I350), COD_CTA + COD_CCUS (I355) devem ser únicos.",
                Evidence = evidence,
            });
    }
}
