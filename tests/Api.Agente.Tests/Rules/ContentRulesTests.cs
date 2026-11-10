using Api.Agente.Application.Rules.Content;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Infrastructure.Ecd;

namespace Api.Agente.Tests.Rules;

public sealed class ContentRulesTests
{
    [Theory]
    [InlineData("ecds-teste/ERRO_01_CNPJ_INVALIDO.txt", nameof(ContentInvalidCnpjRule))]
    [InlineData("ecds-teste/ERRO_02_DATA_INVALIDA.txt", nameof(ContentSpedDateRule))]
    [InlineData("ecds-teste/ERRO_03_PARTIDA_DESBALANCEADA.txt", nameof(ContentBalancedI200Rule))]
    [InlineData("ecds-teste/ERRO_04_CONTA_INEXISTENTE.txt", nameof(ContentI250AccountExistsRule))]
    [InlineData("ecds-teste/ERRO_05_CONTA_DUPLICADA.txt", nameof(ContentDuplicateAccountRule))]
    [InlineData("ecds-teste/ERRO_06_NATUREZA_CONTA_INVALIDA.txt", nameof(ContentAccountNatureRule))]
    [InlineData("ecds-teste/ERRO_07_REGISTRO_I010_AUSENTE.txt", nameof(ContentI010RequiredRule))]
    [InlineData("ecds-teste/ERRO_11_CONTA_PAI_INEXISTENTE.txt", nameof(ContentAccountParentExistsRule))]
    [InlineData("ecds-teste/ERRO_08_ORDEM_REGISTROS.txt", nameof(ContentBlockIRecordOrderRule))]
    [InlineData("ecds-teste/ERRO_09_VALOR_NAO_NUMERICO.txt", nameof(ContentSpedNumericAmountRule))]
    [InlineData("ecds-teste/ERRO_10_CONTADORES_INCORRETOS.txt", nameof(ContentI990And9999CountersRule))]
    [InlineData("ecds-teste/ERRO_12_LANCAMENTO_DUPLICADO.txt", nameof(ContentDuplicateI200Rule))]
    public async Task ContentRules_FlagExpectedEcdFixtures(string relativePath, string ruleName)
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
            nameof(ContentInvalidCnpjRule) => new ContentInvalidCnpjRule(),
            nameof(ContentSpedDateRule) => new ContentSpedDateRule(),
            nameof(ContentBalancedI200Rule) => new ContentBalancedI200Rule(),
            nameof(ContentI010RequiredRule) => new ContentI010RequiredRule(),
            nameof(ContentI250AccountExistsRule) => new ContentI250AccountExistsRule(),
            nameof(ContentDuplicateAccountRule) => new ContentDuplicateAccountRule(),
            nameof(ContentAccountNatureRule) => new ContentAccountNatureRule(),
            nameof(ContentAccountParentExistsRule) => new ContentAccountParentExistsRule(),
            nameof(ContentBlockIRecordOrderRule) => new ContentBlockIRecordOrderRule(),
            nameof(ContentSpedNumericAmountRule) => new ContentSpedNumericAmountRule(),
            nameof(ContentI990And9999CountersRule) => new ContentI990And9999CountersRule(),
            nameof(ContentDuplicateI200Rule) => new ContentDuplicateI200Rule(),
            _ => throw new ArgumentOutOfRangeException(nameof(ruleName)),
        };

        var result = await rule.EvaluateAsync(context);
        Assert.Equal(RuleOutcome.Error, result.Outcome);
    }

    [Theory]
    [InlineData("ecds-teste/ECD_001_BASICA.txt")]
    [InlineData("ecds-teste/ECD_002_VOLUME.txt")]
    [InlineData("ecds-teste/ECD_003_CENTROS_CUSTO.txt")]
    [InlineData("ecds-teste/ECD_004_ENCERRAMENTO.txt")]
    [InlineData("ecds-teste/ECD_005_COMPLEXA.txt")]
    public async Task I990And9999Rule_AcceptsValidEcdFixtures(string relativePath)
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

        var result = await new ContentI990And9999CountersRule().EvaluateAsync(context);
        Assert.Equal(RuleOutcome.Validated, result.Outcome);
    }

    [Fact]
    public async Task InvalidCnpjRule_AcceptsValidFixture()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "ecds-teste", "ECD_001_BASICA.txt");
        var lines = LoadLines(await File.ReadAllTextAsync(path));
        var context = new EcdEvaluationContext
        {
            ExecutionId = Guid.NewGuid(),
            Lines = lines,
            HasOpeningBlock = true,
        };

        var result = await new ContentInvalidCnpjRule().EvaluateAsync(context);
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
            if (Directory.Exists(Path.Combine(dir, "ecds-teste")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
