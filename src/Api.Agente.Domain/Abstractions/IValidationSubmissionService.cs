using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Abstractions;

public sealed record ValidationSubmissionResult(
    Guid ExecutionId,
    ValidationStatus Status,
    string StatusUrl);

public interface IValidationSubmissionService
{
    Task<ValidationSubmissionResult> SubmitAsync(
        Stream fileContent,
        string fileName,
        string contentType,
        string userId,
        string? correlationId,
        CancellationToken cancellationToken = default);
}
