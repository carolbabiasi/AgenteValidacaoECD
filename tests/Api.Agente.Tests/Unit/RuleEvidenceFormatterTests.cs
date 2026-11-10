using Api.Agente.Application.Ecd;
using Api.Agente.Domain.ValueObjects;

namespace Api.Agente.Tests.Unit;

public sealed class RuleEvidenceFormatterTests
{
    [Fact]
    public void ToExplanationStorage_JoinsMultipleLinesForSofia()
    {
        var evidence = new[]
        {
            new RuleEvidence
            {
                LineNumber = 13,
                RecordType = "I200",
                Message = "Lançamento I200 duplicado (data 15012025, nº 0001, histórico «RECEBIMENTO»)",
                FoundValue = "15012025|0001",
            },
            new RuleEvidence
            {
                LineNumber = 16,
                RecordType = "I200",
                Message = "Lançamento I200 duplicado (data 15012025, nº 0001, histórico «DUPLICADO»)",
                FoundValue = "15012025|0001",
            },
        };

        var stored = RuleEvidenceFormatter.ToExplanationStorage(evidence);
        var roundTrip = RuleEvidenceFormatter.ParseExplanationLines(stored);

        Assert.Equal(2, roundTrip.Count);
        Assert.Contains("linha 13", roundTrip[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("linha 16", roundTrip[1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatCounter_UsesReadablePortuguese()
    {
        var evidence = new[]
        {
            new RuleEvidence
            {
                LineNumber = 16,
                RecordType = "I990",
                FieldName = "QTD_LIN",
                FoundValue = "15",
                ExpectedValue = "13",
                Message = "ignored",
            },
            new RuleEvidence
            {
                LineNumber = 21,
                RecordType = "9999",
                FieldName = "QTD_LIN_ARQ",
                FoundValue = "22",
                ExpectedValue = "21",
                Message = "ignored",
            },
        };

        var lines = RuleEvidenceFormatter.ToDisplayLines(evidence);
        Assert.Equal(2, lines.Count);
        Assert.Contains("Bloco I (I990)", lines[0], StringComparison.Ordinal);
        Assert.Contains("15", lines[0], StringComparison.Ordinal);
        Assert.Contains("13", lines[0], StringComparison.Ordinal);
        Assert.DoesNotContain("QTD_LIN", lines[0], StringComparison.Ordinal);
        Assert.Contains("Encerramento do arquivo (9999)", lines[1], StringComparison.Ordinal);
    }
}
