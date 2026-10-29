using Api.Rag.Domain.Enums;

namespace Api.Rag.Domain.Entities;

public sealed class IngestionJob
{
    public Guid Id { get; set; }

    public Guid ManualDocumentId { get; set; }

    public ManualDocument? ManualDocument { get; set; }

    public IngestionJobStatus Status { get; set; }

    public string? ErrorLog { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
