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

    public int ChunksCreated { get; set; }

    public int ChunksSkipped { get; set; }

    public bool ActivateOnSuccess { get; set; }
}
