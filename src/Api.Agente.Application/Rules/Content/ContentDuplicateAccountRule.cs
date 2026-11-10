using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentDuplicateAccountRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-006";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentDuplicateAccountRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var accounts = EcdChartOfAccountsIndex.Parse(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var group in accounts.GroupBy(a => a.CodConta, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            foreach (var entry in group)
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I050",
                    LineNumber = entry.LineNumber,
                    FieldName = "COD_CTA",
                    FoundValue = entry.CodConta,
                    Message = $"Conta '{entry.CodConta}' duplicada no plano I050.",
                });
            }
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Nenhuma conta duplicada no I050.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = $"{evidence.Count} registro(s) I050 com conta duplicada.",
            Evidence = evidence,
        });
    }
}
