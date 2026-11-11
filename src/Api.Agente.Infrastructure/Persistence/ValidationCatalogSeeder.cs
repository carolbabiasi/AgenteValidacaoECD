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
        catch (Exception ex)
        {
            // Falha no seed não deve derrubar o host (ex.: coluna varchar desatualizada em produção).
            logger.LogError(ex, "Falha ao semear catálogo de regras; API permanece no ar.");
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
            new ValidationRule
            {
                Id = OfficialPvaVlLctoEscAuxiliarRule.RuleId,
                Version = OfficialPvaVlLctoEscAuxiliarRule.RuleVersion,
                Name = "VL_LCTO vs I250 (escrituração auxiliar)",
                Description =
                    "IND_ESC = A: soma dos débitos ou dos créditos (I250) deve igualar VL_LCTO (campo 04) do I200.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.Medium,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR",
                ImplementationKey = nameof(OfficialPvaVlLctoEscAuxiliarRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaVlLctoEscAuxiliarRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaObrigNaoMfI200Rule.RuleId,
                Version = OfficialPvaObrigNaoMfI200Rule.RuleVersion,
                Name = "VL_LCTO obrigatório (não MF)",
                Description = "VL_LCTO (campo 04 do I200) deve ser diferente de zero quando IDENT_MF ≠ S.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_OBRIG_NAO_MF_I200",
                ImplementationKey = nameof(OfficialPvaObrigNaoMfI200Rule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaObrigNaoMfI200Rule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaRegistroDuplicadoI200Rule.RuleId,
                Version = OfficialPvaRegistroDuplicadoI200Rule.RuleVersion,
                Name = "NUM_LCTO duplicado (I200)",
                Description = "NUM_LCTO (campo 02) deve ser único entre registros I200.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_REGISTRO_DUPLICADO (I200)",
                ImplementationKey = nameof(OfficialPvaRegistroDuplicadoI200Rule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaRegistroDuplicadoI200Rule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaLcto4FormulaRule.RuleId,
                Version = OfficialPvaLcto4FormulaRule.RuleVersion,
                Name = "Lançamento normal (4ª fórmula)",
                Description =
                    "IND_LCTO=N: menos de 2 partidas D e menos de 2 partidas C no I250 gera aviso.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.Medium,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_LCTO_4_FORMULA",
                ImplementationKey = nameof(OfficialPvaLcto4FormulaRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaLcto4FormulaRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaHistoricoObrigatorioRule.RuleId,
                Version = OfficialPvaHistoricoObrigatorioRule.RuleVersion,
                Name = "Histórico obrigatório I250",
                Description = "Partida I250 exige HIST (08) ou COD_HIST_PAD (07).",
                Category = RuleCategory.Structural,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_HISTORICO_OBRIGATORIO",
                ImplementationKey = nameof(OfficialPvaHistoricoObrigatorioRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaHistoricoObrigatorioRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoValorDebRule.RuleId,
                Version = OfficialPvaValidacaoValorDebRule.RuleVersion,
                Name = "Σ débitos vs VL_DEB (I155)",
                Description = "Soma débitos I250 por período/conta = VL_DEB (I155) em G/R/A.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_VALOR_DEB",
                ImplementationKey = nameof(OfficialPvaValidacaoValorDebRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaValidacaoValorDebRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoValorCredRule.RuleId,
                Version = OfficialPvaValidacaoValorCredRule.RuleVersion,
                Name = "Σ créditos vs VL_CRED (I155)",
                Description = "Soma créditos I250 por período/conta = VL_CRED (I155) em G/R/A.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_VALOR_CRED",
                ImplementationKey = nameof(OfficialPvaValidacaoValorCredRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaValidacaoValorCredRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoSaldoFinalRule.RuleId,
                Version = OfficialPvaValidacaoSaldoFinalRule.RuleVersion,
                Name = "Saldo final I155",
                Description = "VL_SLD_FIN coerente com VL_SLD_INI, VL_DEB e VL_CRED.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_SALDO_FINAL",
                ImplementationKey = nameof(OfficialPvaValidacaoSaldoFinalRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaValidacaoSaldoFinalRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoDebDifCredRule.RuleId,
                Version = OfficialPvaValidacaoDebDifCredRule.RuleVersion,
                Name = "Σ débitos = Σ créditos (I155)",
                Description = "Por período I150 (G/R): soma VL_DEB = soma VL_CRED nos I155.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_DEB_DIF_CRED",
                ImplementationKey = nameof(OfficialPvaValidacaoDebDifCredRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaValidacaoDebDifCredRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoSomaSaldoInicialRule.RuleId,
                Version = OfficialPvaValidacaoSomaSaldoInicialRule.RuleVersion,
                Name = "Σ saldo inicial zero",
                Description = "Por I150 (G/R): soma assinada dos VL_SLD_INI = 0.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_SOMA_SALDO_INICIAL",
                ImplementationKey = nameof(OfficialPvaValidacaoSomaSaldoInicialRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaValidacaoSomaSaldoInicialRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoSomaSaldoFinalRule.RuleId,
                Version = OfficialPvaValidacaoSomaSaldoFinalRule.RuleVersion,
                Name = "Σ saldo final zero",
                Description = "Por I150 (G/R): soma assinada dos VL_SLD_FIN = 0.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_SOMA_SALDO_FINAL",
                ImplementationKey = nameof(OfficialPvaValidacaoSomaSaldoFinalRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaValidacaoSomaSaldoFinalRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaRegistroObrigatorioI350Rule.RuleId,
                Version = OfficialPvaRegistroObrigatorioI350Rule.RuleVersion,
                Name = "I200 encerramento obrigatório (I350)",
                Description = "I350 informado ⇒ I200 com IND_LCTO=E na mesma DT_RES (G/R).",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_REGISTRO_OBRIGATORIO_I350",
                ImplementationKey = nameof(OfficialPvaRegistroObrigatorioI350Rule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaRegistroObrigatorioI350Rule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoSaldoContaRule.RuleId,
                Version = OfficialPvaValidacaoSaldoContaRule.RuleVersion,
                Name = "Saldo conta vs encerramento (I355)",
                Description = "Σ lançamentos E por DT_RES/conta = VL_CTA (I355) com IND_DC invertido.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_SALDO_CONTA",
                ImplementationKey = nameof(OfficialPvaValidacaoSaldoContaRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaValidacaoSaldoContaRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoContaResultadoRule.RuleId,
                Version = OfficialPvaValidacaoContaResultadoRule.RuleVersion,
                Name = "Soma saldos de resultado no encerramento",
                Description =
                    "Na DT_RES (I350), soma dos VL_SLD_FIN das contas COD_NAT=04 (I155) deve ser zero.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_CONTA_RESULTADO",
                ImplementationKey = nameof(OfficialPvaValidacaoContaResultadoRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding
                    {
                        RuleId = OfficialPvaValidacaoContaResultadoRule.RuleId,
                        LayoutVersion = "*",
                    },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaDtResDuplicidadeRule.RuleId,
                Version = OfficialPvaDtResDuplicidadeRule.RuleVersion,
                Name = "DT_RES única (I350)",
                Description = "DT_RES (campo 02) não pode repetir em registros I350.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_DT_RES_DUPLICIDADE",
                ImplementationKey = nameof(OfficialPvaDtResDuplicidadeRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaDtResDuplicidadeRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaCodCtaDtResDuplicidadeRule.RuleId,
                Version = OfficialPvaCodCtaDtResDuplicidadeRule.RuleVersion,
                Name = "Conta+CCUS únicos por DT_RES (I355)",
                Description = "Por DT_RES, COD_CTA + COD_CCUS únicos em I355.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_COD_CTA_DT_RES_DUPLICIDADE",
                ImplementationKey = nameof(OfficialPvaCodCtaDtResDuplicidadeRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding
                    {
                        RuleId = OfficialPvaCodCtaDtResDuplicidadeRule.RuleId,
                        LayoutVersion = "*",
                    },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaEncerramentoExercicioRule.RuleId,
                Version = OfficialPvaEncerramentoExercicioRule.RuleVersion,
                Name = "I350 na DT_EX_SOCIAL",
                Description = "DT_EX_SOCIAL no período (G/R/B) ⇒ I350 com DT_RES igual.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_ENCERRAMENTO_EXERCICIO",
                ImplementationKey = nameof(OfficialPvaEncerramentoExercicioRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaEncerramentoExercicioRule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaContaResultadoI355Rule.RuleId,
                Version = OfficialPvaContaResultadoI355Rule.RuleVersion,
                Name = "Conta de resultado no I355",
                Description = "I355.COD_CTA com COD_NAT=04 no I050.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_CONTA_RESULTADO",
                ImplementationKey = nameof(OfficialPvaContaResultadoI355Rule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding { RuleId = OfficialPvaContaResultadoI355Rule.RuleId, LayoutVersion = "*" },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaDataIntervaloDoArquivoRule.RuleId,
                Version = OfficialPvaDataIntervaloDoArquivoRule.RuleVersion,
                Name = "Datas no intervalo do 0000",
                Description = "I150/I200/I350 dentro de DT_INI..DT_FIN (0000).",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_DATA_INTERVALO_DO_ARQUIVO",
                ImplementationKey = nameof(OfficialPvaDataIntervaloDoArquivoRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding
                    {
                        RuleId = OfficialPvaDataIntervaloDoArquivoRule.RuleId,
                        LayoutVersion = "*",
                    },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaDuplicidadeContaSaldoPeriodicoRule.RuleId,
                Version = OfficialPvaDuplicidadeContaSaldoPeriodicoRule.RuleVersion,
                Name = "Duplicidade I155 por período",
                Description = "COD_CTA + COD_CCUS únicos por I150 no I155.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO",
                ImplementationKey = nameof(OfficialPvaDuplicidadeContaSaldoPeriodicoRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding
                    {
                        RuleId = OfficialPvaDuplicidadeContaSaldoPeriodicoRule.RuleId,
                        LayoutVersion = "*",
                    },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule.RuleId,
                Version = OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule.RuleVersion,
                Name = "I155 zerado sem I157",
                Description = "Sem I157 filho, ao menos um campo de saldo/movimento ≠ 0.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.Medium,
                OfficialReference =
                    "Manual ECD Leiaute 9 (jan/2026) — REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO",
                ImplementationKey = nameof(OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding
                    {
                        RuleId = OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule.RuleId,
                        LayoutVersion = "*",
                    },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidacaoSaldoIniDifFinRule.RuleId,
                Version = OfficialPvaValidacaoSaldoIniDifFinRule.RuleVersion,
                Name = "VL_SLD_INI = VL_SLD_FIN anterior",
                Description = "2º período I150+: VL_SLD_INI = VL_SLD_FIN do período anterior.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 (jan/2026) — REGRA_VALIDACAO_SALDO_INI_DIF_FIN",
                ImplementationKey = nameof(OfficialPvaValidacaoSaldoIniDifFinRule),
                IsActive = true,
                LayoutBindings =
                [
                    new RuleLayoutBinding
                    {
                        RuleId = OfficialPvaValidacaoSaldoIniDifFinRule.RuleId,
                        LayoutVersion = "*",
                    },
                ],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidaSldIniSomaSldIniI157Rule.RuleId,
                Version = OfficialPvaValidaSldIniSomaSldIniI157Rule.RuleVersion,
                Name = "I155 vs soma I157",
                Description = "VL_SLD_INI I155 = soma VL_SLD_INI I157.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_VALIDA_SLD_INI_SOMA_SLD_INI_I157",
                ImplementationKey = nameof(OfficialPvaValidaSldIniSomaSldIniI157Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaValidaSldIniSomaSldIniI157Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaContinuidadeSaldosPeriodicosRule.RuleId,
                Version = OfficialPvaContinuidadeSaldosPeriodicosRule.RuleVersion,
                Name = "Continuidade saldos I155",
                Description = "I155 para todos os meses do 0000.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_CONTINUIDADE_SALDOS_PERIODICOS",
                ImplementationKey = nameof(OfficialPvaContinuidadeSaldosPeriodicosRule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaContinuidadeSaldosPeriodicosRule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaValidaMesI157Rule.RuleId,
                Version = OfficialPvaValidaMesI157Rule.RuleVersion,
                Name = "Mês I150 vs 0000 (I157)",
                Description = "Com I157, mês DT_INI I150 = mês DT_INI 0000.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_VALIDA_MES_I157",
                ImplementationKey = nameof(OfficialPvaValidaMesI157Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaValidaMesI157Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaExisteI157PeriodoPosteriorRule.RuleId,
                Version = OfficialPvaExisteI157PeriodoPosteriorRule.RuleVersion,
                Name = "I157 no 1º período",
                Description = "Com I157, 1º I150.DT_INI = 0000.DT_INI.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_EXISTE_I157_PERIODO_POSTERIOR",
                ImplementationKey = nameof(OfficialPvaExisteI157PeriodoPosteriorRule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaExisteI157PeriodoPosteriorRule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaNaturezaContaPaiI157Rule.RuleId,
                Version = OfficialPvaNaturezaContaPaiI157Rule.RuleVersion,
                Name = "Natureza I157/C155",
                Description = "Natureza I157/C155 compatível com I155.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_NATUREZA_CONTA_PAI_I157",
                ImplementationKey = nameof(OfficialPvaNaturezaContaPaiI157Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaNaturezaContaPaiI157Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule.RuleId,
                Version = OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule.RuleVersion,
                Name = "Duplicidade I150",
                Description = "Chave DT_INI+DT_FIN única no I150.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_DUPLICIDADE_PERIODO_SALDO_PERIODICO",
                ImplementationKey = nameof(OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaDataMesI150Rule.RuleId,
                Version = OfficialPvaDataMesI150Rule.RuleVersion,
                Name = "I150 mesmo mês",
                Description = "DT_INI e DT_FIN do I150 no mesmo mês.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_DATA_MES",
                ImplementationKey = nameof(OfficialPvaDataMesI150Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaDataMesI150Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaContaI155InexistenteC155Rule.RuleId,
                Version = OfficialPvaContaI155InexistenteC155Rule.RuleVersion,
                Name = "I155 no C155 recuperado",
                Description = "Sem mudança de plano, I155 deve existir no C155.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_CONTA_I155_INEXISTENTE_C155",
                ImplementationKey = nameof(OfficialPvaContaI155InexistenteC155Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaContaI155InexistenteC155Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaContaC155InexistenteI155Rule.RuleId,
                Version = OfficialPvaContaC155InexistenteI155Rule.RuleVersion,
                Name = "C155 exige I155 (cenários)",
                Description = "C155 com saldo nas hipóteses do manual ⇒ I155 (e I157 no cenário 4).",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_CONTA_C155_INEXISTENTE_I155",
                ImplementationKey = nameof(OfficialPvaContaC155InexistenteI155Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaContaC155InexistenteI155Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaExisteI155ComI157NoC155Rule.RuleId,
                Version = OfficialPvaExisteI155ComI157NoC155Rule.RuleVersion,
                Name = "I157 exige C155 com saldo",
                Description = "I157 com saldo inicial ⇒ C155 mesma conta com saldo final ≠ 0.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_EXISTE_I155_COM_I157_NO_C155",
                ImplementationKey = nameof(OfficialPvaExisteI155ComI157NoC155Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaExisteI155ComI157NoC155Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaNaturezaContaC155Rule.RuleId,
                Version = OfficialPvaNaturezaContaC155Rule.RuleVersion,
                Name = "Natureza C155 vs I050",
                Description = "C050.COD_NAT igual a I050 para conta no C155/I155.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_NATUREZA_CONTA_C155",
                ImplementationKey = nameof(OfficialPvaNaturezaContaC155Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaNaturezaContaC155Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaNaturezaContaI155Rule.RuleId,
                Version = OfficialPvaNaturezaContaI155Rule.RuleVersion,
                Name = "Natureza I155 vs C050",
                Description = "I050.COD_NAT coerente com C050 na mesma conta recuperada.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_NATUREZA_CONTA_I155",
                ImplementationKey = nameof(OfficialPvaNaturezaContaI155Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaNaturezaContaI155Rule.RuleId, LayoutVersion = "*" }],
            },
            new ValidationRule
            {
                Id = OfficialPvaContaI157InexistenteC155Rule.RuleId,
                Version = OfficialPvaContaI157InexistenteC155Rule.RuleVersion,
                Name = "I157 no C155 (mudança plano)",
                Description = "Com mudança de plano, contas I157 devem existir no C155.",
                Category = RuleCategory.Accounting,
                SeverityDefault = FindingSeverity.High,
                OfficialReference = "Manual ECD Leiaute 9 — REGRA_CONTA_I157_INEXISTENTE",
                ImplementationKey = nameof(OfficialPvaContaI157InexistenteC155Rule),
                IsActive = true,
                LayoutBindings = [new RuleLayoutBinding { RuleId = OfficialPvaContaI157InexistenteC155Rule.RuleId, LayoutVersion = "*" }],
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
