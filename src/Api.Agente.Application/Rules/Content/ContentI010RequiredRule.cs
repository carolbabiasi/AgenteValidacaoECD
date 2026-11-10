using Api.Agente.Application.Ecd;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Content;

public sealed class ContentI010RequiredRule : IValidationRule
{
    public const string RuleId = "ECD-CONTENT-003";
    public const string RuleVersion = "1.0.0";
    public string ImplementationKey => nameof(ContentI010RequiredRule);

    private static readonly HashSet<string> BlockIRecords =
    [
        "I001", "I010", "I030", "I050", "I150", "I155", "I200", "I250", "I990",
    ];

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        var hasBlockI = context.Lines.Any(l => BlockIRecords.Contains(l.RecordType ?? string.Empty));
        var hasI010 = context.Lines.Any(l => l.RecordType == "I010");

        if (!hasBlockI || hasI010)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = hasBlockI ? "Registro I010 presente." : "Bloco I ausente.",
            });
        }

        var firstI = context.Lines.First(l => BlockIRecords.Contains(l.RecordType ?? string.Empty));
        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = "Falta o registro I010 no Bloco I.",
            Evidence =
            [
                new RuleEvidence
                {
                    RecordType = firstI.RecordType,
                    LineNumber = firstI.LineNumber,
                    Message =
                        $"Na linha {firstI.LineNumber}, o Bloco I começa sem o registro I010 (identificação da escrituração contábil), obrigatório antes dos demais registros.",
                },
            ],
        });
    }
}
