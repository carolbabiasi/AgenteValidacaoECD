using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Agente.Tests.Fixtures;
using Api.Agente.Tests.Support;

namespace Api.Agente.Tests.Integration;

[Collection(AgenteTestsCollection.Name)]
public sealed class SofiaA2aContractTests
{
    private readonly AgenteWebApplicationFactory _factory;

    public SofiaA2aContractTests(AgenteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AgentCard_IsPublic()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/.well-known/agent-card.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validar_ecd_sofia", json.GetProperty("skills")[0].GetProperty("id").GetString());
        Assert.Contains("/a2a", json.GetProperty("url").GetString());
    }

    [Fact]
    public async Task MessageSend_ReturnsCompletedTaskWithResultado()
    {
        using var client = CreateA2aClient();
        var idExecucao = Guid.NewGuid();
        var idSolicitacao = Guid.NewGuid();
        var payload = new
        {
            versaoContrato = "1.0",
            idExecucao,
            idSolicitacao,
            usuario = new { nome = "Maria SOFIA" },
            pergunta = "Validar ECD de teste",
            documentos = new[]
            {
                new
                {
                    idDocumento = "doc-happy",
                    nome = "ecd-happy.txt",
                    texto = SampleEcdFiles.ValidMinimal + $"\n|correlation|{Guid.NewGuid():N}|\n",
                },
            },
        };

        var envelope = new
        {
            jsonrpc = "2.0",
            id = "rpc-test-001",
            method = "message/send",
            @params = new
            {
                message = new
                {
                    role = "user",
                    messageId = Guid.NewGuid().ToString(),
                    parts = new[]
                    {
                        new { kind = "text", text = JsonSerializer.Serialize(payload) },
                    },
                },
            },
        };

        var response = await client.PostAsJsonAsync("/a2a", envelope);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", body.GetProperty("result").GetProperty("status").GetProperty("state").GetString());

        var resultadoText = body.GetProperty("result").GetProperty("artifacts")[0]
            .GetProperty("parts")[0].GetProperty("text").GetString();
        Assert.NotNull(resultadoText);
        var resultado = JsonSerializer.Deserialize<JsonElement>(resultadoText);
        Assert.Equal(idExecucao.ToString(), resultado.GetProperty("idExecucao").GetString());
        Assert.Equal(idSolicitacao.ToString(), resultado.GetProperty("idSolicitacao").GetString());
    }

    private HttpClient CreateA2aClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DevJwtTokenFactory.CreateValidatorToken("sofia-a2a"));
        return client;
    }
}
