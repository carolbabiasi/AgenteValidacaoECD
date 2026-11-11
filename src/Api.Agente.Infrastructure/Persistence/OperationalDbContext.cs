using Api.Agente.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Agente.Infrastructure.Persistence;

public sealed class OperationalDbContext(DbContextOptions<OperationalDbContext> options) : DbContext(options)
{
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    public DbSet<ValidationExecution> ValidationExecutions => Set<ValidationExecution>();

    public DbSet<ValidationJob> ValidationJobs => Set<ValidationJob>();

    public DbSet<ValidationRule> ValidationRules => Set<ValidationRule>();

    public DbSet<RuleLayoutBinding> RuleLayoutBindings => Set<RuleLayoutBinding>();

    public DbSet<RuleRunResult> RuleRunResults => Set<RuleRunResult>();

    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("operational");

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sha256).HasMaxLength(64).IsFixedLength();
            entity.Property(e => e.OriginalFileName).HasMaxLength(512);
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.StoragePath).HasMaxLength(1024);
            entity.HasIndex(e => e.Sha256);
        });

        modelBuilder.Entity<ValidationExecution>(entity =>
        {
            entity.ToTable("ValidationExecutions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.SubmittedByUserId).HasMaxLength(128);
            entity.Property(e => e.OrganizationId).HasMaxLength(64);
            entity.Property(e => e.DetectedLayoutVersion).HasMaxLength(32);
            entity.Property(e => e.ManualVersionUsed).HasMaxLength(64);
            entity.Property(e => e.LlmResponseSchemaVersion).HasMaxLength(16);
            entity.Property(e => e.ErrorCode).HasMaxLength(64);
            entity.Property(e => e.ProgressPhase).HasMaxLength(64);
            entity.HasOne(e => e.OriginalFile)
                .WithMany()
                .HasForeignKey(e => e.OriginalFileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<ValidationJob>(entity =>
        {
            entity.ToTable("ValidationJobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.State).IsConcurrencyToken();
            entity.HasOne(e => e.Execution)
                .WithMany(e => e.Jobs)
                .HasForeignKey(e => e.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.State, e.LockedUntil });
            entity.HasIndex(e => new { e.ExecutionId, e.Attempt }).IsUnique();
        });

        modelBuilder.Entity<ValidationRule>(entity =>
        {
            entity.ToTable("ValidationRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Version).HasMaxLength(16);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.OfficialReference).HasMaxLength(512);
            entity.Property(e => e.ImplementationKey).HasMaxLength(128);
        });

        modelBuilder.Entity<RuleLayoutBinding>(entity =>
        {
            entity.ToTable("RuleLayoutBindings");
            entity.HasKey(e => new { e.RuleId, e.LayoutVersion });
            entity.Property(e => e.RuleId).HasMaxLength(64);
            entity.Property(e => e.LayoutVersion).HasMaxLength(32);
            entity.HasOne(e => e.Rule)
                .WithMany(r => r.LayoutBindings)
                .HasForeignKey(e => e.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleRunResult>(entity =>
        {
            entity.ToTable("RuleRunResults");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleId).HasMaxLength(64);
            entity.Property(e => e.RuleVersion).HasMaxLength(16);
            entity.HasOne(e => e.Execution)
                .WithMany(e => e.RuleRunResults)
                .HasForeignKey(e => e.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Finding>(entity =>
        {
            entity.ToTable("Findings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrimaryRecordType).HasMaxLength(10);
            entity.Property(e => e.Summary).HasMaxLength(1024);
            entity.HasOne(e => e.Execution)
                .WithMany(e => e.Findings)
                .HasForeignKey(e => e.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
