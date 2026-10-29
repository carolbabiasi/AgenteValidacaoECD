using Api.Agente.Application.Rules.Samples;
using Api.Agente.Infrastructure.Ecd;
using Api.Agente.Domain.Ecd;
using Api.Agente.Domain.Enums;
using Api.Agente.Tests.Fixtures;

namespace Api.Agente.Tests.Rules;

public sealed class SampleRulesTests
{
    [Fact]
    public async Task MissingOpeningBlockRule_FlagsAbsence()
    {
        var rule = new SampleMissingOpeningBlockRule();
        var context = new EcdEvaluationContext
        {
            ExecutionId = Guid.NewGuid(),
            Lines = Parse(SampleEcdFiles.MissingOpening),
            HasOpeningBlock = false,
        };

        var result = await rule.EvaluateAsync(context);

        Assert.Equal(RuleOutcome.Error, result.Outcome);
    }

    [Fact]
    public async Task MalformedLineRule_FlagsInvalidLine()
    {
        var rule = new SampleMalformedLineRule();
        var lines = Parse(SampleEcdFiles.MalformedLine);
        var context = new EcdEvaluationContext
        {
            ExecutionId = Guid.NewGuid(),
            Lines = lines,
            HasOpeningBlock = lines.Any(l => l.RecordType == "0000"),
        };

        var result = await rule.EvaluateAsync(context);

        Assert.Equal(RuleOutcome.Error, result.Outcome);
    }

    private static IReadOnlyList<EcdLine> Parse(string content)
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
}
