using Api.Agente.Application.Rules.OfficialPva;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Infrastructure.Ecd;

namespace Api.Agente.Tests.Rules;

public sealed class OfficialPvaRulesTests
{
    [Theory]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_DEB_OK.txt", nameof(OfficialPvaVlLctoDebRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_DEB_ERRO.txt", nameof(OfficialPvaVlLctoDebRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva-leiaute9/PVA_REGRA_VALIDACAO_VL_LCTO_DEB_ERRO.txt", nameof(OfficialPvaVlLctoDebRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_DEB_OK.txt", nameof(OfficialPvaVlLctoCredRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO.txt", nameof(OfficialPvaVlLctoCredRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_OK.txt", nameof(OfficialPvaVlLctoEscAuxiliarRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_ERRO.txt", nameof(OfficialPvaVlLctoEscAuxiliarRule), RuleOutcome.Alert)]
    [InlineData("ecds-teste-pva-leiaute9/PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO.txt", nameof(OfficialPvaVlLctoCredRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva-leiaute9/PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_ERRO.txt", nameof(OfficialPvaVlLctoEscAuxiliarRule), RuleOutcome.Alert)]
    [InlineData("ecds-teste-pva/PVA_REGRA_OBRIG_NAO_MF_I200_OK.txt", nameof(OfficialPvaObrigNaoMfI200Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_OBRIG_NAO_MF_I200_ERRO.txt", nameof(OfficialPvaObrigNaoMfI200Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_REGISTRO_DUPLICADO_ERRO.txt", nameof(OfficialPvaRegistroDuplicadoI200Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_LCTO_4_FORMULA_OK.txt", nameof(OfficialPvaLcto4FormulaRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_LCTO_4_FORMULA_ERRO.txt", nameof(OfficialPvaLcto4FormulaRule), RuleOutcome.Alert)]
    [InlineData("ecds-teste-pva/PVA_REGRA_HISTORICO_OBRIGATORIO_OK.txt", nameof(OfficialPvaHistoricoObrigatorioRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_HISTORICO_OBRIGATORIO_ERRO.txt", nameof(OfficialPvaHistoricoObrigatorioRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VALOR_DEB_OK.txt", nameof(OfficialPvaValidacaoValorDebRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VALOR_DEB_ERRO.txt", nameof(OfficialPvaValidacaoValorDebRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VALOR_DEB_OK.txt", nameof(OfficialPvaValidacaoValorCredRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VALOR_CRED_ERRO.txt", nameof(OfficialPvaValidacaoValorCredRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SALDO_FINAL_OK.txt", nameof(OfficialPvaValidacaoSaldoFinalRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SALDO_FINAL_ERRO.txt", nameof(OfficialPvaValidacaoSaldoFinalRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_DEB_DIF_CRED_OK.txt", nameof(OfficialPvaValidacaoDebDifCredRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_DEB_DIF_CRED_ERRO.txt", nameof(OfficialPvaValidacaoDebDifCredRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SOMA_SALDO_INICIAL_OK.txt", nameof(OfficialPvaValidacaoSomaSaldoInicialRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SOMA_SALDO_INICIAL_ERRO.txt", nameof(OfficialPvaValidacaoSomaSaldoInicialRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SOMA_SALDO_FINAL_OK.txt", nameof(OfficialPvaValidacaoSomaSaldoFinalRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SOMA_SALDO_FINAL_ERRO.txt", nameof(OfficialPvaValidacaoSomaSaldoFinalRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_REGISTRO_OBRIGATORIO_I350_OK.txt", nameof(OfficialPvaRegistroObrigatorioI350Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_REGISTRO_OBRIGATORIO_I350_ERRO.txt", nameof(OfficialPvaRegistroObrigatorioI350Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SALDO_CONTA_OK.txt", nameof(OfficialPvaValidacaoSaldoContaRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SALDO_CONTA_ERRO.txt", nameof(OfficialPvaValidacaoSaldoContaRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_CONTA_RESULTADO_OK.txt", nameof(OfficialPvaValidacaoContaResultadoRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_CONTA_RESULTADO_ERRO.txt", nameof(OfficialPvaValidacaoContaResultadoRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DT_RES_DUPLICIDADE_OK.txt", nameof(OfficialPvaDtResDuplicidadeRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DT_RES_DUPLICIDADE_ERRO.txt", nameof(OfficialPvaDtResDuplicidadeRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_COD_CTA_DT_RES_DUPLICIDADE_OK.txt", nameof(OfficialPvaCodCtaDtResDuplicidadeRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_COD_CTA_DT_RES_DUPLICIDADE_ERRO.txt", nameof(OfficialPvaCodCtaDtResDuplicidadeRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_ENCERRAMENTO_EXERCICIO_OK.txt", nameof(OfficialPvaEncerramentoExercicioRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_ENCERRAMENTO_EXERCICIO_ERRO.txt", nameof(OfficialPvaEncerramentoExercicioRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_RESULTADO_I355_OK.txt", nameof(OfficialPvaContaResultadoI355Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_RESULTADO_I355_ERRO.txt", nameof(OfficialPvaContaResultadoI355Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DATA_INTERVALO_DO_ARQUIVO_OK.txt", nameof(OfficialPvaDataIntervaloDoArquivoRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DATA_INTERVALO_DO_ARQUIVO_ERRO.txt", nameof(OfficialPvaDataIntervaloDoArquivoRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO_OK.txt", nameof(OfficialPvaDuplicidadeContaSaldoPeriodicoRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DUPLICIDADE_CONTA_SALDO_PERIODICO_ERRO.txt", nameof(OfficialPvaDuplicidadeContaSaldoPeriodicoRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO_OK.txt", nameof(OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CAMPOS_SALDOS_PERIODICOS_DIFERENTE_ZERO_ERRO.txt", nameof(OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule), RuleOutcome.Alert)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SALDO_INI_DIF_FIN_OK.txt", nameof(OfficialPvaValidacaoSaldoIniDifFinRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_SALDO_INI_DIF_FIN_ERRO.txt", nameof(OfficialPvaValidacaoSaldoIniDifFinRule), RuleOutcome.Error)]
    public async Task OfficialPvaRules_Fixtures(
        string relativePath,
        string ruleName,
        RuleOutcome expectedOutcome)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var lines = LoadLines(await File.ReadAllTextAsync(path));
        var context = new EcdEvaluationContext
        {
            ExecutionId = Guid.NewGuid(),
            Lines = lines,
            HasOpeningBlock = lines.Any(l => l.RecordType == "0000"),
        };

        IValidationRule rule = ruleName switch
        {
            nameof(OfficialPvaVlLctoDebRule) => new OfficialPvaVlLctoDebRule(),
            nameof(OfficialPvaVlLctoCredRule) => new OfficialPvaVlLctoCredRule(),
            nameof(OfficialPvaVlLctoEscAuxiliarRule) => new OfficialPvaVlLctoEscAuxiliarRule(),
            nameof(OfficialPvaObrigNaoMfI200Rule) => new OfficialPvaObrigNaoMfI200Rule(),
            nameof(OfficialPvaRegistroDuplicadoI200Rule) => new OfficialPvaRegistroDuplicadoI200Rule(),
            nameof(OfficialPvaLcto4FormulaRule) => new OfficialPvaLcto4FormulaRule(),
            nameof(OfficialPvaHistoricoObrigatorioRule) => new OfficialPvaHistoricoObrigatorioRule(),
            nameof(OfficialPvaValidacaoValorDebRule) => new OfficialPvaValidacaoValorDebRule(),
            nameof(OfficialPvaValidacaoValorCredRule) => new OfficialPvaValidacaoValorCredRule(),
            nameof(OfficialPvaValidacaoSaldoFinalRule) => new OfficialPvaValidacaoSaldoFinalRule(),
            nameof(OfficialPvaValidacaoDebDifCredRule) => new OfficialPvaValidacaoDebDifCredRule(),
            nameof(OfficialPvaValidacaoSomaSaldoInicialRule) => new OfficialPvaValidacaoSomaSaldoInicialRule(),
            nameof(OfficialPvaValidacaoSomaSaldoFinalRule) => new OfficialPvaValidacaoSomaSaldoFinalRule(),
            nameof(OfficialPvaRegistroObrigatorioI350Rule) => new OfficialPvaRegistroObrigatorioI350Rule(),
            nameof(OfficialPvaValidacaoSaldoContaRule) => new OfficialPvaValidacaoSaldoContaRule(),
            nameof(OfficialPvaValidacaoContaResultadoRule) => new OfficialPvaValidacaoContaResultadoRule(),
            nameof(OfficialPvaDtResDuplicidadeRule) => new OfficialPvaDtResDuplicidadeRule(),
            nameof(OfficialPvaCodCtaDtResDuplicidadeRule) => new OfficialPvaCodCtaDtResDuplicidadeRule(),
            nameof(OfficialPvaEncerramentoExercicioRule) => new OfficialPvaEncerramentoExercicioRule(),
            nameof(OfficialPvaContaResultadoI355Rule) => new OfficialPvaContaResultadoI355Rule(),
            nameof(OfficialPvaDataIntervaloDoArquivoRule) => new OfficialPvaDataIntervaloDoArquivoRule(),
            nameof(OfficialPvaDuplicidadeContaSaldoPeriodicoRule) => new OfficialPvaDuplicidadeContaSaldoPeriodicoRule(),
            nameof(OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule) => new OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule(),
            nameof(OfficialPvaValidacaoSaldoIniDifFinRule) => new OfficialPvaValidacaoSaldoIniDifFinRule(),
            _ => throw new ArgumentOutOfRangeException(nameof(ruleName)),
        };

        var result = await rule.EvaluateAsync(context);
        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.Equal(ruleName switch
        {
            nameof(OfficialPvaVlLctoDebRule) => OfficialPvaVlLctoDebRule.RuleId,
            nameof(OfficialPvaVlLctoCredRule) => OfficialPvaVlLctoCredRule.RuleId,
            nameof(OfficialPvaVlLctoEscAuxiliarRule) => OfficialPvaVlLctoEscAuxiliarRule.RuleId,
            nameof(OfficialPvaObrigNaoMfI200Rule) => OfficialPvaObrigNaoMfI200Rule.RuleId,
            nameof(OfficialPvaRegistroDuplicadoI200Rule) => OfficialPvaRegistroDuplicadoI200Rule.RuleId,
            nameof(OfficialPvaLcto4FormulaRule) => OfficialPvaLcto4FormulaRule.RuleId,
            nameof(OfficialPvaHistoricoObrigatorioRule) => OfficialPvaHistoricoObrigatorioRule.RuleId,
            nameof(OfficialPvaValidacaoValorDebRule) => OfficialPvaValidacaoValorDebRule.RuleId,
            nameof(OfficialPvaValidacaoValorCredRule) => OfficialPvaValidacaoValorCredRule.RuleId,
            nameof(OfficialPvaValidacaoSaldoFinalRule) => OfficialPvaValidacaoSaldoFinalRule.RuleId,
            nameof(OfficialPvaValidacaoDebDifCredRule) => OfficialPvaValidacaoDebDifCredRule.RuleId,
            nameof(OfficialPvaValidacaoSomaSaldoInicialRule) => OfficialPvaValidacaoSomaSaldoInicialRule.RuleId,
            nameof(OfficialPvaValidacaoSomaSaldoFinalRule) => OfficialPvaValidacaoSomaSaldoFinalRule.RuleId,
            nameof(OfficialPvaRegistroObrigatorioI350Rule) => OfficialPvaRegistroObrigatorioI350Rule.RuleId,
            nameof(OfficialPvaValidacaoSaldoContaRule) => OfficialPvaValidacaoSaldoContaRule.RuleId,
            nameof(OfficialPvaValidacaoContaResultadoRule) => OfficialPvaValidacaoContaResultadoRule.RuleId,
            nameof(OfficialPvaDtResDuplicidadeRule) => OfficialPvaDtResDuplicidadeRule.RuleId,
            nameof(OfficialPvaCodCtaDtResDuplicidadeRule) => OfficialPvaCodCtaDtResDuplicidadeRule.RuleId,
            nameof(OfficialPvaEncerramentoExercicioRule) => OfficialPvaEncerramentoExercicioRule.RuleId,
            nameof(OfficialPvaContaResultadoI355Rule) => OfficialPvaContaResultadoI355Rule.RuleId,
            nameof(OfficialPvaDataIntervaloDoArquivoRule) => OfficialPvaDataIntervaloDoArquivoRule.RuleId,
            nameof(OfficialPvaDuplicidadeContaSaldoPeriodicoRule) => OfficialPvaDuplicidadeContaSaldoPeriodicoRule.RuleId,
            nameof(OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule) => OfficialPvaCamposSaldosPeriodicosDiferenteZeroRule.RuleId,
            nameof(OfficialPvaValidacaoSaldoIniDifFinRule) => OfficialPvaValidacaoSaldoIniDifFinRule.RuleId,
            _ => string.Empty,
        }, result.RuleId);
    }

    [Fact]
    public async Task VlLctoDebRule_IgnoraQuandoIndEscAuxiliar()
    {
        var content = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "ecds-teste-pva", "PVA_REGRA_VALIDACAO_VL_LCTO_DEB_ERRO.txt"));
        var mutated = content.Replace("|I010|G|", "|I010|A|", StringComparison.Ordinal);
        var lines = LoadLines(mutated);
        var context = new EcdEvaluationContext
        {
            ExecutionId = Guid.NewGuid(),
            Lines = lines,
            HasOpeningBlock = true,
        };

        var result = await new OfficialPvaVlLctoDebRule().EvaluateAsync(context);
        Assert.Equal(RuleOutcome.Validated, result.Outcome);
    }

    private static List<EcdLine> LoadLines(string content)
    {
        var list = new List<EcdLine>();
        var n = 0;
        foreach (var raw in content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            n++;
            list.Add(new EcdLine(n, raw, StreamingEcdFileReader.ParseRecordType(raw)));
        }

        return list;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "ecds-teste-pva")))
            {
                return dir;
            }

            if (Directory.Exists(Path.Combine(dir, "ecds-teste-pva-leiaute9")) ||
                Directory.Exists(Path.Combine(dir, "ecds-teste")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
