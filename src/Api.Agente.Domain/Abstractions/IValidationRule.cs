using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Domain.Abstractions;

public interface IValidationRule
{
    string ImplementationKey { get; }

    Task<RuleEvaluationResult> EvaluateAsync(EcdEvaluationContext context, CancellationToken cancellationToken = default);
}
