using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Samples;

public sealed class SampleMalformedLineRule : IValidationRule
{
    public const string RuleId = "ECD-SAMPLE-001";
    public const string RuleVersion = "0.1.0";
    public string ImplementationKey => nameof(SampleMalformedLineRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var malformed = context.Lines
            .Where(l => !l.RawText.StartsWith('|') || l.RecordType is null)
            .Take(20)
            .Select(l => new RuleEvidence
            {
                LineNumber = l.LineNumber,
                FoundValue = l.RawText.Length > 120 ? l.RawText[..120] + "…" : l.RawText,
                Message = "Linha fora do padrão SPED (pipe-delimited).",
            })
            .ToList();

        if (malformed.Count == 0)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Nenhuma linha malformada detectada (SAMPLE).",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = $"{malformed.Count} linha(s) malformada(s) detectada(s) (SAMPLE).",
            Evidence = malformed,
        });
    }
}
