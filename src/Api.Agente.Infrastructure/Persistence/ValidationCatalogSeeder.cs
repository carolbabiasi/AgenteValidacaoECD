using Api.Agente.Application.Rules.Content;
using Api.Agente.Application.Rules.OfficialPva;
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
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            await SeedCoreAsync(cancellationToken);
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private async Task SeedCoreAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OperationalDbContext>();

        logger.LogInformation("Semeando catálogo SAMPLE de regras ECD (idempotente)");

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
            new ValidationRule
            {
                Id = ContentInvalidCnpjRule.RuleId,
                Version = ContentInvalidCnpjRule.RuleVersion,
                Name = "CNPJ inválido (0000/I030)",
                Description = "Valida dígitos verificadores de CNPJ nos registros de abertura.",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentInvalidCnpjRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentInvalidCnpjRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentSpedDateRule.RuleId,
                Version = ContentSpedDateRule.RuleVersion,
                Name = "Data SPED inválida",
                Description = "Verifica formato e calendário ddMMyyyy em registros-chave.",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentSpedDateRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentSpedDateRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentI010RequiredRule.RuleId,
                Version = ContentI010RequiredRule.RuleVersion,
                Name = "I010 obrigatório",
                Description = "Exige registro I010 quando o Bloco I está presente.",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentI010RequiredRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentI010RequiredRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentBalancedI200Rule.RuleId,
                Version = ContentBalancedI200Rule.RuleVersion,
                Name = "Partida I200 balanceada",
                Description = "Soma de débitos e créditos por lançamento I250.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentBalancedI200Rule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentBalancedI200Rule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentI250AccountExistsRule.RuleId,
                Version = ContentI250AccountExistsRule.RuleVersion,
                Name = "Conta inexistente em lançamento",
                Description = "Valida COD_CTA em I155/I250 contra plano I050.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentI250AccountExistsRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentI250AccountExistsRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentDuplicateAccountRule.RuleId,
                Version = ContentDuplicateAccountRule.RuleVersion,
                Name = "Conta duplicada no plano",
                Description = "Detecta COD_CTA repetido em I050.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentDuplicateAccountRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentDuplicateAccountRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentAccountNatureRule.RuleId,
                Version = ContentAccountNatureRule.RuleVersion,
                Name = "Natureza de conta inválida",
                Description = "NAT_CONTA em I050 deve ser 01 a 05.",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentAccountNatureRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentAccountNatureRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentAccountParentExistsRule.RuleId,
                Version = ContentAccountParentExistsRule.RuleVersion,
                Name = "Conta superior inexistente",
                Description = "COD_CTA_SUP de I050 deve existir no plano.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentAccountParentExistsRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentAccountParentExistsRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentBlockIRecordOrderRule.RuleId,
                Version = ContentBlockIRecordOrderRule.RuleVersion,
                Name = "Ordem Bloco I",
                Description = "I150 deve preceder o primeiro I200.",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentBlockIRecordOrderRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentBlockIRecordOrderRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentSpedNumericAmountRule.RuleId,
                Version = ContentSpedNumericAmountRule.RuleVersion,
                Name = "Valor I250 numérico",
                Description = "VL_DEB_CRE em I250 com indicador D/C deve ser numérico.",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentSpedNumericAmountRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentSpedNumericAmountRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentI990And9999CountersRule.RuleId,
                Version = ContentI990And9999CountersRule.RuleVersion,
                Name = "Contadores I990 e 9999",
                Description = "Quantidades declaradas em I990 e 9999.",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentI990And9999CountersRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentI990And9999CountersRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = ContentDuplicateI200Rule.RuleId,
                Version = ContentDuplicateI200Rule.RuleVersion,
                Name = "Lançamento I200 duplicado",
                Description = "Data+número repetidos em I200.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                ImplementationKey = nameof(ContentDuplicateI200Rule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = ContentDuplicateI200Rule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaVlLctoDebRule.RuleId,
                Version = OfficialPvaVlLctoDebRule.RuleVersion,
                Name = "VL_LCTO vs soma débitos I250",
                Description =
                    "IND_ESC ≠ A: soma dos débitos das partidas deve igualar VL_LCTO (campo 04) do I200.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_VL_LCTO_DEB",
                ImplementationKey = nameof(OfficialPvaVlLctoDebRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaVlLctoDebRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaVlLctoCredRule.RuleId,
                Version = OfficialPvaVlLctoCredRule.RuleVersion,
                Name = "VL_LCTO vs soma créditos I250",
                Description =
                    "IND_ESC ≠ A: soma dos créditos das partidas deve igualar VL_LCTO (campo 04) do I200.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_VL_LCTO_CRED",
                ImplementationKey = nameof(OfficialPvaVlLctoCredRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaVlLctoCredRule.RuleId, LayoutVersion = "*" },
                ],
            },
        };

        foreach (var rule in rules)
        {
            if (await db.ValidationRules.AnyAsync(r => r.Id == rule.Id, cancellationToken))
            {
                continue;
            }

            db.ValidationRules.Add(rule);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
