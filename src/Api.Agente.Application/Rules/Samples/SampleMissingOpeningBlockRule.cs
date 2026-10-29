using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Application.Rules.Samples;

public sealed class SampleMissingOpeningBlockRule : IValidationRule
{
    public const string RuleId = "ECD-SAMPLE-002";
    public const string RuleVersion = "0.1.0";
    public string ImplementationKey => nameof(SampleMissingOpeningBlockRule);

    public Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (context.HasOpeningBlock)
        {
            return Task.FromResult(new RuleEvaluationResult
            {
                RuleId = RuleId,
                RuleVersion = RuleVersion,
                Outcome = RuleOutcome.Validated,
                Message = "Registro de abertura 0000 presente (SAMPLE).",
            });
        }

        return Task.FromResult(new RuleEvaluationResult
        {
            RuleId = RuleId,
            RuleVersion = RuleVersion,
            Outcome = RuleOutcome.Error,
            Message = "Registro de abertura |0000| ausente (SAMPLE).",
            Evidence =
            [
                new RuleEvidence
                {
                    RecordType = "0000",
                    Message = "Esperado registro 0000 no início do arquivo ECD.",
                },
            ],
        });
    }
}
