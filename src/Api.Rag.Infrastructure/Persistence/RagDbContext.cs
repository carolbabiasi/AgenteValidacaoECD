using Api.Rag.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Api.Rag.Infrastructure.Persistence;

public sealed class RagDbContext(DbContextOptions<RagDbContext> options) : DbContext(options)
{
    public DbSet<ManualDocument> ManualDocuments => Set<ManualDocument>();

    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasDefaultSchema("rag");

        modelBuilder.Entity<ManualDocument>(entity =>
        {
            entity.ToTable("ManualDocuments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(512);
            entity.Property(e => e.ManualVersion).HasMaxLength(32);
            entity.Property(e => e.LayoutFamily).HasMaxLength(32);
            entity.Property(e => e.SourceUri).HasMaxLength(2048);
            entity.Property(e => e.DocumentCode)
                .HasMaxLength(ManualDocument.MaxDocumentCodeLength)
                .HasDefaultValue(ManualDocument.DefaultManualCode);
            entity.HasIndex(e => new { e.ManualVersion, e.LayoutFamily, e.Status });
            entity.HasIndex(e => new { e.LayoutFamily, e.DocumentCode, e.Status });
        });

        modelBuilder.Entity<IngestionJob>(entity =>
        {
            entity.ToTable("IngestionJobs");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ManualDocument)
                .WithMany(e => e.IngestionJobs)
                .HasForeignKey(e => e.ManualDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("DocumentChunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChunkKey).HasMaxLength(128);
            entity.Property(e => e.ContentHash).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.HierarchyPath).HasColumnType("text");
            entity.Property(e => e.SourceLocation).HasColumnType("text");
            entity.Property(e => e.Content).HasColumnType("text");
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(1536)")
                .HasConversion(
                    v => v == null ? null : new Vector(v),
                    v => v == null ? null : v.ToArray());
            entity.HasOne(e => e.ManualDocument)
                .WithMany(e => e.Chunks)
                .HasForeignKey(e => e.ManualDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ManualDocumentId, e.ChunkKey }).IsUnique();
        });
    }
}
