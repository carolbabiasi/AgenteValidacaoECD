using Api.Agente.Application.Sofia;
using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;

namespace Api.Agente.Tests.Unit;

public sealed class SofiaObservacoesBuilderTests
{
    [Fact]
    public void Build_ProducesReadableLinesWithoutTechnicalKeys()
    {
        var execution = new ValidationExecution
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr",
            SubmittedByUserId = "u",
            OriginalFileId = Guid.NewGuid(),
            Status = ValidationStatus.Completed,
            DetectedLayoutVersion = "LECD",
            ManualVersionUsed = "ECD-L9-2026-05",
        };

        var reportJson = """
            {
              "findings": [
                {
                  "ragReferences": [
                    {
                      "manualVersion": "ECD-L9-2026-05",
                      "hierarchyPath": "Capítulo 3 – Blocos > Registro I990: Encerramento do Bloco I"
                    }
                  ]
                }
              ]
            }
            """;

        var lines = SofiaObservacoesBuilder.Build(execution, reportJson);

        Assert.Contains(lines, l => l.StartsWith("Leiaute detectado:", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("Leiaute 9", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("Referência no manual:", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("Registro I990", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.Contains("executionId=", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.StartsWith("rag=", StringComparison.Ordinal));
    }
}
