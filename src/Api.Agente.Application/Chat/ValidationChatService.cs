using System.Text;
using System.Text.Json;
using Api.Agente.Domain.Abstractions;
using Api.Agente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Api.Agente.Application.Chat;

public sealed class ValidationChatService(
    IValidationExecutionRepository executions,
    IChatSessionStore sessionStore,
    IExecutionAuthorization authorization,
    ILogger<ValidationChatService> logger) : IValidationChatService
{
    private const string Disclaimer =
        "Orientação profissional; não altera arquivo fonte.";

    public async Task<ValidationChatResponse> AskAsync(
        Guid executionId,
        ValidationChatRequest request,
        CancellationToken cancellationToken = default)
    {
        await authorization.AuthorizeAgentOperationsAsync(executionId, cancellationToken);

        var execution = await executions.GetWithFindingsAsync(executionId, cancellationToken)
            ?? throw new KeyNotFoundException("NOT_FOUND");

        if (execution.Status != ValidationStatus.Completed)
        {
            throw new InvalidOperationException("CHAT_NOT_READY: aguarde conclusão da validação.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("message obrigatória.");
        }

        var sessionId = request.SessionId ?? Guid.NewGuid();
        await sessionStore.AppendAsync(sessionId, new ChatMessage("user", request.Message.Trim(), DateTimeOffset.UtcNow), cancellationToken);

        var answer = BuildDeterministicAnswer(execution, request.Message);
        await sessionStore.AppendAsync(sessionId, new ChatMessage("assistant", answer, DateTimeOffset.UtcNow), cancellationToken);

        logger.LogInformation("Chat execução {ExecutionId} sessão {SessionId}", executionId, sessionId);

        return new ValidationChatResponse(
            sessionId,
            answer,
            Array.Empty<object>(),
            Disclaimer);
    }

    private static string BuildDeterministicAnswer(Domain.Entities.ValidationExecution execution, string message)
    {
        var sb = new StringBuilder();
        sb.Append("Resposta orientativa (sem LLM em homolog): ");
        sb.Append(message.Trim());
        sb.Append(". Achados registrados: ");
        sb.Append(execution.FindingsCount);

        if (!string.IsNullOrWhiteSpace(execution.ReportJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(execution.ReportJson);
                if (doc.RootElement.TryGetProperty("findings", out var findings) && findings.GetArrayLength() > 0)
                {
                    var first = findings[0];
                    if (first.TryGetProperty("title", out var title))
                    {
                        sb.Append(". Exemplo: ");
                        sb.Append(title.GetString());
                    }
                }
            }
            catch (JsonException)
            {
                // ignore parse issues in chat stub
            }
        }

        return sb.ToString();
    }
}
