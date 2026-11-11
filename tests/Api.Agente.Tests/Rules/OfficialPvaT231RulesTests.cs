using Api.Agente.Application.Rules.OfficialPva;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Infrastructure.Ecd;

namespace Api.Agente.Tests.Rules;

public sealed class OfficialPvaT231RulesTests
{
    [Theory]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_SLD_INI_SOMA_SLD_INI_I157_OK.txt", nameof(OfficialPvaValidaSldIniSomaSldIniI157Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_SLD_INI_SOMA_SLD_INI_I157_ERRO.txt", nameof(OfficialPvaValidaSldIniSomaSldIniI157Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTINUIDADE_SALDOS_PERIODICOS_OK.txt", nameof(OfficialPvaContinuidadeSaldosPeriodicosRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTINUIDADE_SALDOS_PERIODICOS_ERRO.txt", nameof(OfficialPvaContinuidadeSaldosPeriodicosRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_MES_I157_OK.txt", nameof(OfficialPvaValidaMesI157Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_MES_I157_ERRO.txt", nameof(OfficialPvaValidaMesI157Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_MES_I157_OK.txt", nameof(OfficialPvaExisteI157PeriodoPosteriorRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_EXISTE_I157_PERIODO_POSTERIOR_ERRO.txt", nameof(OfficialPvaExisteI157PeriodoPosteriorRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DUPLICIDADE_PERIODO_SALDO_PERIODICO_ERRO.txt", nameof(OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTINUIDADE_SALDOS_PERIODICOS_OK.txt", nameof(OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_DATA_MES_I150_ERRO.txt", nameof(OfficialPvaDataMesI150Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDA_MES_I157_OK.txt", nameof(OfficialPvaDataMesI150Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_I155_INEXISTENTE_C155_OK.txt", nameof(OfficialPvaContaI155InexistenteC155Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_I155_INEXISTENTE_C155_ERRO.txt", nameof(OfficialPvaContaI155InexistenteC155Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_I155_INEXISTENTE_C155_OK.txt", nameof(OfficialPvaContaC155InexistenteI155Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_C155_INEXISTENTE_I155_ERRO.txt", nameof(OfficialPvaContaC155InexistenteI155Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_EXISTE_I155_COM_I157_NO_C155_OK.txt", nameof(OfficialPvaExisteI155ComI157NoC155Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_EXISTE_I155_COM_I157_NO_C155_ERRO.txt", nameof(OfficialPvaExisteI155ComI157NoC155Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_NATUREZA_CONTA_C155_OK.txt", nameof(OfficialPvaNaturezaContaC155Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_NATUREZA_CONTA_C155_ERRO.txt", nameof(OfficialPvaNaturezaContaC155Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_NATUREZA_CONTA_I155_OK.txt", nameof(OfficialPvaNaturezaContaI155Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_NATUREZA_CONTA_I155_ERRO.txt", nameof(OfficialPvaNaturezaContaI155Rule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_I157_INEXISTENTE_OK.txt", nameof(OfficialPvaContaI157InexistenteC155Rule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_CONTA_I157_INEXISTENTE_ERRO.txt", nameof(OfficialPvaContaI157InexistenteC155Rule), RuleOutcome.Error)]
    public async Task T231Rules_Fixtures(string relativePath, string ruleName, RuleOutcome expectedOutcome)
    {
        var path = Path.Combine(OfficialPvaRulesTestsHelpers.FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
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
        nameof(OfficialPvaValidaSldIniSomaSldIniI157Rule) => new OfficialPvaValidaSldIniSomaSldIniI157Rule(),
        nameof(OfficialPvaContinuidadeSaldosPeriodicosRule) => new OfficialPvaContinuidadeSaldosPeriodicosRule(),
        nameof(OfficialPvaValidaMesI157Rule) => new OfficialPvaValidaMesI157Rule(),
        nameof(OfficialPvaExisteI157PeriodoPosteriorRule) => new OfficialPvaExisteI157PeriodoPosteriorRule(),
        nameof(OfficialPvaNaturezaContaPaiI157Rule) => new OfficialPvaNaturezaContaPaiI157Rule(),
        nameof(OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule) => new OfficialPvaDuplicidadePeriodoSaldoPeriodicoRule(),
        nameof(OfficialPvaDataMesI150Rule) => new OfficialPvaDataMesI150Rule(),
        nameof(OfficialPvaContaI155InexistenteC155Rule) => new OfficialPvaContaI155InexistenteC155Rule(),
        nameof(OfficialPvaContaC155InexistenteI155Rule) => new OfficialPvaContaC155InexistenteI155Rule(),
        nameof(OfficialPvaExisteI155ComI157NoC155Rule) => new OfficialPvaExisteI155ComI157NoC155Rule(),
        nameof(OfficialPvaNaturezaContaC155Rule) => new OfficialPvaNaturezaContaC155Rule(),
        nameof(OfficialPvaNaturezaContaI155Rule) => new OfficialPvaNaturezaContaI155Rule(),
        nameof(OfficialPvaContaI157InexistenteC155Rule) => new OfficialPvaContaI157InexistenteC155Rule(),
        _ => throw new ArgumentOutOfRangeException(nameof(ruleName)),
    };
}

internal static class OfficialPvaRulesTestsHelpers
{
    public static List<EcdLine> LoadLines(string content)
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

    public static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "ecds-teste-pva")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Não foi possível localizar a raiz do repositório.");
    }
}
