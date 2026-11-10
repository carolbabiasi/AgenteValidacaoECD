using System.Text.Json;
using Api.Agente.Application.Sofia;
using Api.Agente.Domain.Sofia;
using Api.Agente.Host.Auth;

namespace Api.Agente.Host.Endpoints;

public static class SofiaA2aEndpoints
{
    public const string AgentId = "agente-validacao-ecd";
    public const string SkillId = "analisar_documentos";

    public static IEndpointRouteBuilder MapSofiaA2aEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/agent-card.json", (IConfiguration configuration, HttpContext http) =>
        {
            if (!SofiaA2aAuthorization.TryAuthorizeA2a(http, configuration, out var failure))
            {
                return failure!;
            }

            var baseUrl = ResolvePublicBaseUrl(configuration, http);
            var card = new
            {
                protocolVersion = "0.3.0",
                preferredTransport = "JSONRPC",
                name = "Agente Validação ECD",
                description = "Valida arquivos ECD (SPED) enviados pela SOFIA via contrato v1.0",
                url = $"{baseUrl.TrimEnd('/')}/a2a",
                version = "1.0.0",
                capabilities = new { streaming = false, pushNotifications = false },
                defaultInputModes = new[] { "text/plain" },
                defaultOutputModes = new[] { "text/plain" },
                skills = new[]
                {
                    new
                    {
                        id = SkillId,
                        name = "Análise documental ECD",
                        description = "Analisa texto SPED ECD e retorna achados estruturados",
                        tags = new[] { "infis", "ecd", "sofia", "sped" },
                    },
                },
                securitySchemes = new
                {
                    sofiaService = new
                    {
                        type = "http",
                        scheme = "bearer",
                    },
                },
                security = new[]
                {
                    new Dictionary<string, string[]> { ["sofiaService"] = [] },
                },
            };

            return Results.Json(card);
        })
        .WithName("SofiaAgentCard");

        app.MapPost("/a2a", HandleA2aRpcAsync)
            .WithName("SofiaA2aRpc");

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

    private static async Task<IResult> HandleA2aRpcAsync(
        HttpContext http,
        IConfiguration configuration,
        SofiaEcdAnalysisService analysis,
        SofiaA2aTaskStore taskStore,
        CancellationToken cancellationToken)
    {
        if (!SofiaA2aAuthorization.IsIntegrationConfigured(configuration))
        {
            return Results.Json(
                JsonRpcError(http, null, -32099, "Integração A2A indisponível."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!SofiaA2aAuthorization.TryAuthorizeA2a(http, configuration, out _))
        {
            http.Response.Headers.WWWAuthenticate = "Bearer";
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

        return request?.Method switch
        {
            "message/send" => await HandleMessageSendAsync(http, request, rpcId, analysis, taskStore, cancellationToken),
            "tasks/get" => HandleTasksGet(http, request, rpcId, taskStore),
            "tasks/cancel" => HandleTasksCancel(http, request, rpcId, taskStore),
            _ => Results.Json(JsonRpcError(http, rpcId, -32601, "Método não suportado.")),
        };
    }

    private static async Task<IResult> HandleMessageSendAsync(
        HttpContext http,
        JsonRpcRequest request,
        JsonElement? rpcId,
        SofiaEcdAnalysisService analysis,
        SofiaA2aTaskStore taskStore,
        CancellationToken cancellationToken)
    {
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

        var task = new
        {
            kind = "task",
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
        };

        taskStore.Put(taskId, task);

        var response = new
        {
            jsonrpc = "2.0",
            id = RpcIdElement(rpcId),
            result = task,
        };

        return Results.Json(response);
    }

    private static IResult HandleTasksGet(
        HttpContext http,
        JsonRpcRequest request,
        JsonElement? rpcId,
        SofiaA2aTaskStore taskStore)
    {
        var taskId = request.Params?.Id;
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return Results.Json(JsonRpcError(http, rpcId, -32602, "params.id obrigatório."));
        }

        if (!taskStore.TryGet(taskId, out var task))
        {
            return Results.Json(JsonRpcError(http, rpcId, -32002, "Tarefa não encontrada."));
        }

        return Results.Json(new
        {
            jsonrpc = "2.0",
            id = RpcIdElement(rpcId),
            result = task,
        });
    }

    private static IResult HandleTasksCancel(
        HttpContext http,
        JsonRpcRequest request,
        JsonElement? rpcId,
        SofiaA2aTaskStore taskStore)
    {
        var taskId = request.Params?.Id;
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return Results.Json(JsonRpcError(http, rpcId, -32602, "params.id obrigatório."));
        }

        if (!taskStore.TryGet(taskId, out _))
        {
            return Results.Json(JsonRpcError(http, rpcId, -32002, "Tarefa não encontrada."));
        }

        return Results.Json(JsonRpcError(http, rpcId, -32003, "Tarefa já concluída; cancelamento não suportado."));
    }

    private static JsonElement RpcIdElement(JsonElement? rpcId) =>
        rpcId is { } id && id.ValueKind != JsonValueKind.Undefined
            ? id
            : JsonSerializer.SerializeToElement("a2a");

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
