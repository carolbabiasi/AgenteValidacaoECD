using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Rules;

namespace Api.Agente.Application.Rules;

public sealed class RuleEngine(
    IRuleCatalogRepository catalog,
    IEnumerable<IValidationRule> rules) : IRuleEngine
{
    public async Task<IReadOnlyList<RuleEvaluationResult>> RunStructuralAsync(
        EcdEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        return await RunCategoryAsync(context, RuleCategory.Structural, cancellationToken);
    }

    public async Task<IReadOnlyList<RuleEvaluationResult>> RunAccountingAsync(
        EcdEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        return await RunCategoryAsync(context, RuleCategory.Accounting, cancellationToken);
    }

    private async Task<IReadOnlyList<RuleEvaluationResult>> RunCategoryAsync(
        EcdEvaluationContext context,
        RuleCategory category,
        CancellationToken cancellationToken)
    {
        var applicable = await catalog.GetActiveRulesAsync(category, context.LayoutVersion, cancellationToken);

        var plugins = rules.ToDictionary(r => r.ImplementationKey, StringComparer.OrdinalIgnoreCase);
        var results = new List<RuleEvaluationResult>();

        foreach (var rule in applicable)
        {
            if (!plugins.TryGetValue(rule.ImplementationKey, out var plugin))
            {
                results.Add(new RuleEvaluationResult
                {
                    RuleId = rule.Id,
                    RuleVersion = rule.Version,
                    Outcome = RuleOutcome.NotVerified,
                    Message = $"Implementação '{rule.ImplementationKey}' não registrada.",
                });
                continue;
            }

            results.Add(await plugin.EvaluateAsync(context, cancellationToken));
        }

        return results;
    }
}
