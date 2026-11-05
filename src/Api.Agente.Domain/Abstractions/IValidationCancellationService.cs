using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Abstractions;

public sealed record ValidationCancellationResult(
    Guid ExecutionId,
    ValidationStatus PreviousStatus,
    ValidationStatus Status,
    DateTimeOffset CancelledAt);

public interface IValidationCancellationService
{
    Task<ValidationCancellationResult> CancelAsync(
        Guid executionId,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
