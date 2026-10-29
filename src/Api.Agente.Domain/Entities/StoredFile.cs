namespace Api.Agente.Domain.Entities;

public sealed class StoredFile
{
    public Guid Id { get; set; }

    public required string Sha256 { get; set; }

    public required string OriginalFileName { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    public required string StoragePath { get; set; }

    public DateTimeOffset UploadedAt { get; set; }
}
