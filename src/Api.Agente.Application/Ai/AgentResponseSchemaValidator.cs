using System.Reflection;
using System.Text.Json;
using Api.Agente.Domain.Abstractions;
using Json.Schema;

namespace Api.Agente.Application.Ai;

public sealed class AgentResponseSchemaValidator : IAgentResponseSchemaValidator
{
    private static readonly JsonSchema Schema = LoadSchema();

    public void Validate(string reportJson)
    {
        using var document = JsonDocument.Parse(reportJson);
        var result = Schema.Evaluate(document.RootElement, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
        });

        if (result.IsValid)
        {
            return;
        }

        var details = result.Details is null
            ? "schema inválido"
            : string.Join("; ", result.Details.Where(d => d.HasErrors).Select(d => d.Errors?.FirstOrDefault().Value ?? d.InstanceLocation.ToString()));

        throw new InvalidOperationException($"AGENT_SCHEMA_INVALID: {details}");
    }

    private static JsonSchema LoadSchema()
    {
        var assembly = typeof(AgentResponseSchemaValidator).Assembly;
        using var stream = assembly.GetManifestResourceStream("agent-response-schema.json")
            ?? throw new InvalidOperationException("Recurso embarcado ausente: agent-response-schema.json");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSchema.FromText(json);
    }
}
