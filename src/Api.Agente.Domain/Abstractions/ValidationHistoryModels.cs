using Api.Agente.Domain.Enums;

namespace Api.Agente.Domain.Abstractions;

public sealed record ValidationHistoryQuery(
    string UserId,
    bool IsAdmin,
    int Page,
    int PageSize,
    DateTimeOffset? From,
    DateTimeOffset? To,
    ValidationStatus? Status);

public sealed record ValidationHistoryPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ValidationHistoryItem> Items);

public sealed record ValidationHistoryItem(
    Guid ExecutionId,
    DateTimeOffset SubmittedAt,
    string? FileName,
    ValidationStatus Status,
    int FindingsCount,
    string? DetectedLayoutVersion);
