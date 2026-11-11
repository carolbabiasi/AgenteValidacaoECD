using Api.Agente.Application.Rules.OfficialPva;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Infrastructure.Ecd;

namespace Api.Agente.Tests.Rules;

public sealed class OfficialPvaJRulesTests
{
    [Theory]
    [InlineData("ecds-teste-pva/PVA_REGRA_OBRIGATORIA_DEMONSTRACAO_OK.txt", nameof(OfficialPvaObrigatoriaDemonstracaoRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_OBRIGATORIA_DEMONSTRACAO_ERRO.txt", nameof(OfficialPvaObrigatoriaDemonstracaoRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_REGISTRO_OBRIGATORIO_J005_FIM_EXERCICIO_OK.txt", nameof(OfficialPvaRegistroObrigatorioJ005FimExercicioRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_REGISTRO_OBRIGATORIO_J005_FIM_EXERCICIO_ERRO.txt", nameof(OfficialPvaRegistroObrigatorioJ005FimExercicioRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_ENC_OBRIGATORIO_OK.txt", nameof(OfficialPvaEncObrigatorioRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_ENC_OBRIGATORIO_ERRO.txt", nameof(OfficialPvaEncObrigatorioRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_PERIODO_SUP_UM_ANO_OK.txt", nameof(OfficialPvaPeriodoSupUmAnoRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_PERIODO_SUP_UM_ANO_ERRO.txt", nameof(OfficialPvaPeriodoSupUmAnoRule), RuleOutcome.Alert)]
    [InlineData("ecds-teste-pva/PVA_REGRA_SOMA_DAS_PARCELAS_BALANCO_INI_OK.txt", nameof(OfficialPvaSomaDasParcelasBalancoIniRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_SOMA_DAS_PARCELAS_BALANCO_INI_ERRO.txt", nameof(OfficialPvaSomaDasParcelasBalancoIniRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_SOMA_DAS_PARCELAS_BALANCO_FIN_OK.txt", nameof(OfficialPvaSomaDasParcelasBalancoFinRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_SOMA_DAS_PARCELAS_BALANCO_FIN_ERRO.txt", nameof(OfficialPvaSomaDasParcelasBalancoFinRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_ATIVO_PASSIVO_INI_OK.txt", nameof(OfficialPvaValidaAtivoPassivoIniRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_ATIVO_PASSIVO_INI_ERRO.txt", nameof(OfficialPvaValidaAtivoPassivoIniRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_ATIVO_PASSIVO_FIN_OK.txt", nameof(OfficialPvaValidaAtivoPassivoFinRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_ATIVO_PASSIVO_FIN_ERRO.txt", nameof(OfficialPvaValidaAtivoPassivoFinRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_BALANCO_SALDO_INI_OK.txt", nameof(OfficialPvaBalancoSaldoIniRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_BALANCO_SALDO_INI_ERRO.txt", nameof(OfficialPvaBalancoSaldoIniRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_BALANCO_SALDO_FIN_OK.txt", nameof(OfficialPvaBalancoSaldoFinRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_BALANCO_SALDO_FIN_ERRO.txt", nameof(OfficialPvaBalancoSaldoFinRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_SOMA_NIVEIS_DRE_OK.txt", nameof(OfficialPvaSomaNiveisDreRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_SOMA_NIVEIS_DRE_ERRO.txt", nameof(OfficialPvaSomaNiveisDreRule), RuleOutcome.Error)]
    public async Task OfficialPvaJRules_Fixtures(string relativePath, string ruleName, RuleOutcome expectedOutcome)
    {
        var root = OfficialPvaRulesTestsHelpers.FindRepoRoot();
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var lines = OfficialPvaRulesTestsHelpers.LoadLines(await File.ReadAllTextAsync(path));
        var context = new EcdEvaluationContext
        {
            ExecutionId = Guid.NewGuid(),
            Lines = lines,
            HasOpeningBlock = lines.Any(l => l.RecordType == "0000"),
        };

        var rule = CreateRule(ruleName);
        var result = await rule.EvaluateAsync(context);
        Assert.Equal(expectedOutcome, result.Outcome);
    }

    private static IValidationRule CreateRule(string ruleName) => ruleName switch
    {
        nameof(OfficialPvaObrigatoriaDemonstracaoRule) => new OfficialPvaObrigatoriaDemonstracaoRule(),
        nameof(OfficialPvaRegistroObrigatorioJ005FimExercicioRule) => new OfficialPvaRegistroObrigatorioJ005FimExercicioRule(),
        nameof(OfficialPvaEncObrigatorioRule) => new OfficialPvaEncObrigatorioRule(),
        nameof(OfficialPvaPeriodoSupUmAnoRule) => new OfficialPvaPeriodoSupUmAnoRule(),
        nameof(OfficialPvaSomaDasParcelasBalancoIniRule) => new OfficialPvaSomaDasParcelasBalancoIniRule(),
        nameof(OfficialPvaSomaDasParcelasBalancoFinRule) => new OfficialPvaSomaDasParcelasBalancoFinRule(),
        nameof(OfficialPvaValidaAtivoPassivoIniRule) => new OfficialPvaValidaAtivoPassivoIniRule(),
        nameof(OfficialPvaValidaAtivoPassivoFinRule) => new OfficialPvaValidaAtivoPassivoFinRule(),
        nameof(OfficialPvaBalancoSaldoIniRule) => new OfficialPvaBalancoSaldoIniRule(),
        nameof(OfficialPvaBalancoSaldoFinRule) => new OfficialPvaBalancoSaldoFinRule(),
        nameof(OfficialPvaSomaNiveisDreRule) => new OfficialPvaSomaNiveisDreRule(),
        _ => throw new ArgumentOutOfRangeException(nameof(ruleName)),
    };
}
