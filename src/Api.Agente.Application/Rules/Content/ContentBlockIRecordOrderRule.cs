using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentBlockIRecordOrderRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-009";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentBlockIRecordOrderRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var i150 = context.Lines.FirstOrDefault(l => l.RecordType == "I150");
        var i200 = context.Lines.FirstOrDefault(l => l.RecordType == "I200");

        if (i150 is null || i200 is null || i150.LineNumber < i200.LineNumber)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Ordem I150/I200 consistente no Bloco I.",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = "Registro I200 anterior ao I150 (ordem inválida no Bloco I).",
            Evidence =
            [
                new RuleEvidence
                {
                    RecordType = "I200",
                    LineNumber = i200.LineNumber,
                    Message = $"I200 na linha {i200.LineNumber} ocorre antes do I150 (linha {i150.LineNumber}).",
                },
            ],
        });
    }
}
