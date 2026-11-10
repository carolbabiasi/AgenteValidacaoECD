using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentAccountNatureRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-007";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentAccountNatureRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var accounts = EcdChartOfAccountsIndex.Parse(context.Lines);
        var evidence = new List<RuleEvidence>();

        foreach (var account in accounts)
        {
            if (EcdChartOfAccountsIndex.IsValidNatureza(account.Natureza))
            {
                continue;
            }

            evidence.Add(new RuleEvidence
            {
                RecordType = "I050",
                LineNumber = account.LineNumber,
                FieldName = "NAT_CONTA",
                FoundValue = account.Natureza,
                Message = "Natureza da conta deve ser 01 a 05.",
            });
        }

        if (evidence.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Naturezas de conta I050 válidas.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = $"{evidence.Count} natureza(s) de conta inválida(s).",
            Evidence = evidence,
        });
    }
}
