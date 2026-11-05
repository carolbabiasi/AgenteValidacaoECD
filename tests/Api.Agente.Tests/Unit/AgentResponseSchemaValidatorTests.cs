using Api.Agente.Application.Ai;
using Api.Agente.Domain.Abstractions;

namespace Api.Agente.Tests.Unit;

public sealed class AgentResponseSchemaValidatorTests
{
    private readonly IAgentResponseSchemaValidator _validator = new AgentResponseSchemaValidator();

    [Fact]
    public void ValidMinimalReport_PassesSchema()
    {
        var executionId = Guid.NewGuid();
        var json = $$"""
            {
              "schemaVersion": "1.0.0",
              "executionId": "{{executionId}}",
              "processingStatus": "Completed",
              "summary": {
                "narrative": "Teste",
                "countsBySeverity": { "High": 1 }
              },
              "findings": [
                {
                  "findingId": "f1",
                  "category": "StructuralError",
                  "severity": "High",
                  "title": "Titulo",
                  "recordRefs": []
                }
              ],
              "limitations": ["nenhuma"],
              "skippedValidations": [],
              "requiresHumanReview": true
            }
            """;

        _validator.Validate(json);
    }

    [Fact]
    public void MissingRequiredField_Throws()
    {
        const string json = """{ "schemaVersion": "1.0.0" }""";
        Assert.Throws<InvalidOperationException>(() => _validator.Validate(json));
    }
}
