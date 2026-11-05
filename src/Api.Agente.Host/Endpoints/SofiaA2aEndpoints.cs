using System.Text.Json;
using Api.Agente.Application.Sofia;
using Api.Agente.Domain.Sofia;
using Api.Agente.Host.Auth;
namespace Api.Agente.Host.Endpoints;

public static class SofiaA2aEndpoints
{
    public const string AgentId = "agente-validacao-ecd";
    public const string SkillId = "validar_ecd_sofia";

    public static IEndpointRouteBuilder MapSofiaA2aEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/agent-card.json", (IConfiguration configuration, HttpContext http) =>
        {
            var baseUrl = ResolvePublicBaseUrl(configuration, http);
            var card = new
            {
                name = "Agente Validação ECD",
                description = "Valida arquivos ECD (SPED) enviados pela SOFIA via contrato v1.0",
                url = $"{baseUrl.TrimEnd('/')}/a2a",
                version = "1.0.0",
                capabilities = new { streaming = false, pushNotifications = false },
                defaultInputModes = new[] { "text" },
                defaultOutputModes = new[] { "text" },
                skills = new[]
                {
                    new
                    {
                        id = SkillId,
                        name = "Validação ECD",
                        description = "Analisa texto SPED ECD e retorna achados estruturados",
                        tags = new[] { "infis", "ecd", "sofia", "sped" },
                    },
                },
            };

            return Results.Json(card);
        })
        .WithName("SofiaAgentCard")
        .AllowAnonymous();

        app.MapPost("/a2a", HandleMessageSendAsync)
            .WithName("SofiaA2aMessageSend");

        return app;
    }

    private static string ResolvePublicBaseUrl(IConfiguration configuration, HttpContext http)
    {
        var configured = configuration["Sofia:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        return $"{http.Request.Scheme}://{http.Request.Host}";
    }

    private static async Task<IResult> HandleMessageSendAsync(
        HttpContext http,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        SofiaEcdAnalysisService analysis,
        CancellationToken cancellationToken)
    {
        if (!SofiaA2aAuthorization.IsAuthorized(http, configuration, environment))
        {
            return Results.Json(
                JsonRpcError(http, null, -32001, "Não autorizado."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        JsonRpcRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(
                http.Request.Body,
                SofiaA2aJson.SerializerOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return Results.Json(JsonRpcError(http, null, -32700, "JSON inválido."));
        }

        var rpcId = request?.Id;
        if (!string.Equals(request?.JsonRpc, "2.0", StringComparison.Ordinal))
        {
            return Results.Json(JsonRpcError(http, rpcId, -32600, "jsonrpc deve ser 2.0."));
        }

        if (!string.Equals(request?.Method, "message/send", StringComparison.Ordinal))
        {
            return Results.Json(JsonRpcError(http, rpcId, -32601, "Método não suportado."));
        }

        var textPart = request.Params?.Message?.Parts?
            .FirstOrDefault(p => string.Equals(p.Kind, "text", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(textPart?.Text))
        {
            return Results.Json(JsonRpcError(http, rpcId, -32602, "params.message.parts[text] obrigatório."));
        }

        SolicitacaoAnaliseSofia? solicitacao;
        try
        {
            solicitacao = JsonSerializer.Deserialize<SolicitacaoAnaliseSofia>(textPart.Text, SofiaA2aJson.SerializerOptions);
        }
        catch (JsonException)
        {
            return Results.Json(JsonRpcError(http, rpcId, -32602, "Payload SOFIA não é JSON válido."));
        }

        var validationErrors = SofiaContractValidator.Validate(solicitacao);
        if (validationErrors.Count > 0)
        {
            return Results.Json(JsonRpcError(http, rpcId, -32602, string.Join(' ', validationErrors)));
        }

        var resultado = await analysis.AnalyzeAsync(solicitacao!, cancellationToken);
        var resultadoJson = JsonSerializer.Serialize(resultado, SofiaA2aJson.SerializerOptions);

        var taskId = $"task-{solicitacao!.IdSolicitacao:N}";
        var contextId = $"ctx-{solicitacao.IdExecucao:N}";

        var response = new
        {
            jsonrpc = "2.0",
            id = rpcId is { } id && id.ValueKind != JsonValueKind.Undefined ? id : JsonSerializer.SerializeToElement("a2a"),
            result = new
            {
                id = taskId,
                contextId,
                status = new { state = "completed" },
                artifacts = new[]
                {
                    new
                    {
                        artifactId = $"art-{solicitacao.IdSolicitacao:N}",
                        name = "analise-sofia-v1",
                        parts = new[]
                        {
                            new { kind = "text", text = resultadoJson },
                        },
                    },
                },
            },
        };

        return Results.Json(response);
    }

    private static object JsonRpcError(HttpContext http, JsonElement? id, int code, string message) =>
        new
        {
            jsonrpc = "2.0",
            id = id is { } element && element.ValueKind != JsonValueKind.Undefined
                ? element
                : JsonSerializer.SerializeToElement(http.TraceIdentifier),
            error = new { code, message },
        };
}
