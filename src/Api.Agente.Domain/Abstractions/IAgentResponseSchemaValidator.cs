namespace Api.Agente.Domain.Abstractions;

public interface IAgentResponseSchemaValidator
{
    void Validate(string reportJson);
}
