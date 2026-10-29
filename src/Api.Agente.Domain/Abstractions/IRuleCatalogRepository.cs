using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Abstractions;

public interface IRuleCatalogRepository
{
    Task<IReadOnlyList<ValidationRule>> GetActiveRulesAsync(
        RuleCategory category,
        string? layoutVersion,
        CancellationToken cancellationToken = default);
}
