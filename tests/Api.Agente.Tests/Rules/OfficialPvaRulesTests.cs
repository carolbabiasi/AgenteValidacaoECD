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
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_DEB_OK.txt", nameof(OfficialPvaVlLctoCredRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_CRED_ERRO.txt", nameof(OfficialPvaVlLctoCredRule), RuleOutcome.Error)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_OK.txt", nameof(OfficialPvaVlLctoEscAuxiliarRule), RuleOutcome.Validated)]
    [InlineData("ecds-teste-pva/PVA_REGRA_VALIDACAO_VL_LCTO_ESC_AUXILIAR_ERRO.txt", nameof(OfficialPvaVlLctoEscAuxiliarRule), RuleOutcome.Alert)]
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
            _ => throw new ArgumentOutOfRangeException(nameof(ruleName)),
        };

        var result = await rule.EvaluateAsync(context);
        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.Equal(ruleName switch
        {
            nameof(OfficialPvaVlLctoDebRule) => OfficialPvaVlLctoDebRule.RuleId,
            nameof(OfficialPvaVlLctoCredRule) => OfficialPvaVlLctoCredRule.RuleId,
            nameof(OfficialPvaVlLctoEscAuxiliarRule) => OfficialPvaVlLctoEscAuxiliarRule.RuleId,
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

            if (Directory.Exists(Path.Combine(dir, "ecds-teste")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
