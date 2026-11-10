using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentI250AccountExistsRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-005";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentI250AccountExistsRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var accounts = EcdChartOfAccountsIndex.BuildCodeSet(EcdChartOfAccountsIndex.Parse(context.Lines));
        if (accounts.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.NotVerified,
                Message = "Plano de contas (I050) ausente; não foi possível validar contas dos lançamentos.",
            });
        }

        var evidence = new List<RuleEvidence>();

        foreach (var line in context.Lines)
        {
            if (line.RecordType is not ("I250" or "I155"))
            {
                continue;
            }

            var cod = EcdFieldParser.GetField(line, 2)?.Trim();
            if (string.IsNullOrWhiteSpace(cod))
            {
                continue;
            }

            if (!accounts.Contains(cod))
            {
                evidence.Add(new RuleEvidence
                {
                    RecordType = line.RecordType,
                    LineNumber = line.LineNumber,
                    FieldName = "COD_CTA",
                    FoundValue = cod,
                    Message =
                        $"Na linha {line.LineNumber}, registro {line.RecordType}: a conta «{cod}» não está cadastrada no plano de contas (I050).",
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
                Message = "Contas referenciadas em I155/I250 existem no plano.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = evidence.Count == 1
                ? "Referência a conta que não existe no plano."
                : $"{evidence.Count} referências a contas ausentes do plano.",
            Evidence = evidence,
        });
    }
}
