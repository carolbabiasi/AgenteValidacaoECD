using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Api.Agente.Infrastructure.Persistence;

public sealed class RuleCatalogRepository(OperationalDbContext db) : IRuleCatalogRepository
{
    public async Task<IReadOnlyList<ValidationRule>> GetActiveRulesAsync(
        RuleCategory category,
        string? layoutVersion,
        CancellationToken cancellationToken = default)
    {
        var layout = layoutVersion ?? "*";
        var rules = await db.ValidationRules
            .AsNoTracking()
            .Include(r => r.LayoutBindings)
            .Where(r => r.IsActive && r.Category == category)
            .ToListAsync(cancellationToken);

        return rules
            .Where(r => r.LayoutBindings.Any(b =>
                b.LayoutVersion == "*" ||
                b.LayoutVersion == layout ||
                (layoutVersion is null && b.LayoutVersion == "*")))
            .ToList();
    }
}
