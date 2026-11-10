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
    internal const string TestA2aToken = "test-sofia-a2a-token";
    private readonly AgenteWebApplicationFactory _factory;

    public SofiaA2aContractTests(AgenteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AgentCard_WithoutBearer_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/.well-known/agent-card.json");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AgentCard_WithBearer_ReturnsCardProfile()
    {
        using var client = CreateA2aClient();
        var response = await client.GetAsync("/.well-known/agent-card.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("0.3.0", json.GetProperty("protocolVersion").GetString());
        Assert.Equal("analisar_documentos", json.GetProperty("skills")[0].GetProperty("id").GetString());
        Assert.Contains("/a2a", json.GetProperty("url").GetString());
    }

    [Fact]
    public async Task MessageSend_WithoutBearer_Returns401()
    {
        using var client = _factory.CreateClient();
        var envelope = MinimalMessageSendEnvelope(Guid.NewGuid(), Guid.NewGuid());
        var response = await client.PostAsJsonAsync("/a2a", envelope);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MessageSend_ReturnsCompletedTaskWithResultado()
    {
        using var client = CreateA2aClient();
        var idExecucao = Guid.NewGuid();
        var idSolicitacao = Guid.NewGuid();
        var envelope = MinimalMessageSendEnvelope(idExecucao, idSolicitacao);

        var response = await client.PostAsJsonAsync("/a2a", envelope);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", body.GetProperty("result").GetProperty("status").GetProperty("state").GetString());
        Assert.Equal("task", body.GetProperty("result").GetProperty("kind").GetString());

        var resultadoText = body.GetProperty("result").GetProperty("artifacts")[0]
            .GetProperty("parts")[0].GetProperty("text").GetString();
        Assert.NotNull(resultadoText);
        var resultado = JsonSerializer.Deserialize<JsonElement>(resultadoText);
        Assert.Equal(idExecucao.ToString(), resultado.GetProperty("idExecucao").GetString());
        Assert.Equal(idSolicitacao.ToString(), resultado.GetProperty("idSolicitacao").GetString());
    }

    [Fact]
    public async Task TasksGet_ReturnsSameTask()
    {
        using var client = CreateA2aClient();
        var idSolicitacao = Guid.NewGuid();
        var send = MinimalMessageSendEnvelope(Guid.NewGuid(), idSolicitacao);
        var sendResponse = await client.PostAsJsonAsync("/a2a", send);
        Assert.Equal(HttpStatusCode.OK, sendResponse.StatusCode);
        var sendBody = await sendResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = sendBody.GetProperty("result").GetProperty("id").GetString();

        var getEnvelope = new
        {
            jsonrpc = "2.0",
            id = "rpc-get-001",
            method = "tasks/get",
            @params = new { id = taskId },
        };

        var getResponse = await client.PostAsJsonAsync("/a2a", getEnvelope);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(taskId, getBody.GetProperty("result").GetProperty("id").GetString());
    }

    private static object MinimalMessageSendEnvelope(Guid idExecucao, Guid idSolicitacao)
    {
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

        return new
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
    }

    private HttpClient CreateA2aClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestA2aToken);
        return client;
    }
}
