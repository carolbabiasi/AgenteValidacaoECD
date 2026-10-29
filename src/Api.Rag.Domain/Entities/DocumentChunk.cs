namespace Api.Rag.Domain.Entities;

public sealed class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid ManualDocumentId { get; set; }

    public ManualDocument? ManualDocument { get; set; }

    public required string ChunkKey { get; set; }

    public required string ContentHash { get; set; }

    public required string HierarchyPath { get; set; }

    public string? SourceLocation { get; set; }

    public required string Content { get; set; }

    public float[]? Embedding { get; set; }
}
