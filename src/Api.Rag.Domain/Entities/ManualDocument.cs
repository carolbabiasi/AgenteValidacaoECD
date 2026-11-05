using Api.Rag.Domain.Enums;

namespace Api.Rag.Domain.Entities;

public sealed class ManualDocument
{
    public const string DefaultManualCode = "MANUAL";

    public const int MaxDocumentCodeLength = 64;

    public Guid Id { get; set; }

    public string? SourceUri { get; set; }

    public required string FileName { get; set; }

    public required string ManualVersion { get; set; }

    public DateOnly? PublicationDate { get; set; }

    public required string LayoutFamily { get; set; }

    public DocumentKind DocumentKind { get; set; }

    public string DocumentCode { get; set; } = DefaultManualCode;

    public ManualDocumentStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = [];

    public ICollection<IngestionJob> IngestionJobs { get; set; } = [];
}
