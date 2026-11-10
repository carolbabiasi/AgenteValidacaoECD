using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentAccountParentExistsRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-008";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentAccountParentExistsRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var accounts = EcdChartOfAccountsIndex.Parse(context.Lines);
        var codes = EcdChartOfAccountsIndex.BuildCodeSet(accounts);
        var evidence = new List<RuleEvidence>();

        foreach (var account in accounts)
        {
            var parent = account.CodContaSup;
            if (string.IsNullOrWhiteSpace(parent) || parent == "0")
            {
                continue;
            }

            if (!codes.Contains(parent))
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = "I050",
                    LineNumber = account.LineNumber,
                    FieldName = "COD_CTA_SUP",
                    FoundValue = parent,
                    Message = $"Conta superior '{parent}' não existe no plano I050.",
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
                Message = "Hierarquia de contas I050 consistente.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = $"{evidence.Count} conta(s) com superior inexistente.",
            Evidence = evidence,
        });
    }
}
