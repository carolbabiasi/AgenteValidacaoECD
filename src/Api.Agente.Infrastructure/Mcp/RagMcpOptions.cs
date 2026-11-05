namespace Api.Agente.Infrastructure.Mcp;

public sealed class RagMcpOptions
{
    public const string SectionName = "Rag";

    public Uri BaseUrl { get; set; } = new("http://localhost:5201");

    public string McpPath { get; set; } = "/mcp";

    public RagMcpJwtOptions Jwt { get; set; } = new();
}

public sealed class RagMcpJwtOptions
{
    public string Issuer { get; set; } = "AgenteValidacaoECD-Rag-Dev";

    public string Audience { get; set; } = "AgenteValidacaoECD-Rag-Dev";

    public string SigningKey { get; set; } = "DEV-ONLY-ECD-RAG-SIGNING-KEY-MIN-32-CHARS!!!!";
}
