using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Domain.Abstractions;

public interface IRuleEngine
{
    Task<IReadOnlyList<RuleEvaluationResult>> RunStructuralAsync(
        EcdEvaluationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RuleEvaluationResult>> RunAccountingAsync(
        EcdEvaluationContext context,
        CancellationToken cancellationToken = default);
}
