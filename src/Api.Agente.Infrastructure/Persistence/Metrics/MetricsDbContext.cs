using Microsoft.EntityFrameworkCore;

namespace Api.Agente.Infrastructure.Persistence.Metrics;

public sealed class MetricsDbContext(DbContextOptions<MetricsDbContext> options) : DbContext(options)
{
    public DbSet<LlmUsageRecord> LlmUsageRecords => Set<LlmUsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("metrics");

        modelBuilder.Entity<LlmUsageRecord>(entity =>
        {
            entity.ToTable("LlmUsageRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.Service).HasMaxLength(64);
            entity.Property(e => e.Provider).HasMaxLength(32);
            entity.Property(e => e.Model).HasMaxLength(128);
            entity.Property(e => e.Operation).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(32);
            entity.Property(e => e.PriceTableRef).HasMaxLength(128);
            entity.Property(e => e.EstimatedCost).HasPrecision(18, 8);
            entity.HasIndex(e => e.ExecutionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.ExecutionId, e.AttemptNumber, e.Operation });
        });
    }
}
