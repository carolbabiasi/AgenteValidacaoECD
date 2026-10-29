using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Infrastructure.Persistence;

public sealed class ValidationCatalogSeeder(
    IServiceScopeFactory scopeFactory,
    ILogger<ValidationCatalogSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperationalDbContext>();

        if (await db.ValidationRules.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Semeando catálogo SAMPLE de regras ECD");

        var rules = new[]
        {
            new ValidationRule
            {
                Id = "ECD-SAMPLE-001",
                Version = "0.1.0",
                Name = "SAMPLE — Linha malformada",
                Description = "Detecta linhas fora do padrão pipe-delimited (não normativa).",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = "SampleMalformedLineRule",
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = "ECD-SAMPLE-001", LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = "ECD-SAMPLE-002",
                Version = "0.1.0",
                Name = "SAMPLE — Abertura 0000",
                Description = "Verifica presença do registro 0000 (não normativa).",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = "SampleMissingOpeningBlockRule",
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = "ECD-SAMPLE-002", LayoutVersion = "*" },
                ],
            },
        };

        db.ValidationRules.AddRange(rules);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
