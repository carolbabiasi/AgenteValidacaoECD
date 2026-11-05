using Api.Agente.Domain.Entities;
using Api.Agente.Domain.Enums;
using Api.Agente.Domain.Exceptions;
using Api.Agente.Infrastructure.Jobs;
using Api.Agente.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Agente.Tests.Unit;

public sealed class ExecutionCancellationTests
{
    [Fact]
    public void Registry_TryCancel_SignalsRegisteredToken()
    {
        var registry = new ExecutionCancellationRegistry();
        var executionId = Guid.NewGuid();

        using var scope = registry.Register(executionId, CancellationToken.None);

        Assert.True(registry.TryCancel(executionId));
        Assert.True(scope.Token.IsCancellationRequested);
    }

    [Fact]
    public void Registry_TryCancel_AfterDispose_ReturnsFalse()
    {
        var registry = new ExecutionCancellationRegistry();
        var executionId = Guid.NewGuid();

        registry.Register(executionId, CancellationToken.None).Dispose();

        Assert.False(registry.TryCancel(executionId));
    }

    [Fact]
    public async Task UpdateProgress_WhenExecutionCancelled_ThrowsAndKeepsCancelledStatus()
    {
        await using var db = CreateDb();
        var executionId = await SeedAsync(db, ValidationStatus.Cancelled);
        var repository = new ValidationExecutionRepository(db);

        await Assert.ThrowsAsync<ExecutionCancelledException>(() =>
            repository.UpdateProgressAsync(executionId, ValidationStatus.Running, "StructuralValidation", 40));

        var status = await db.ValidationExecutions.AsNoTracking()
            .Where(e => e.Id == executionId)
            .Select(e => e.Status)
            .SingleAsync();
        Assert.Equal(ValidationStatus.Cancelled, status);
    }

    [Fact]
    public async Task UpdateProgress_WhenCancelledByAnotherContext_DetectsStaleTrackedEntity()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        await using var pipelineDb = CreateDb(databaseName);
        var executionId = await SeedAsync(pipelineDb, ValidationStatus.Running);
        var repository = new ValidationExecutionRepository(pipelineDb);
        await repository.UpdateProgressAsync(executionId, ValidationStatus.Running, "LayoutDetection", 10);

        await using (var apiDb = CreateDb(databaseName))
        {
            var execution = await apiDb.ValidationExecutions.SingleAsync(e => e.Id == executionId);
            execution.Status = ValidationStatus.Cancelled;
            await apiDb.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<ExecutionCancelledException>(() =>
            repository.UpdateProgressAsync(executionId, ValidationStatus.Completed, "Completed", 100));
    }

    private static OperationalDbContext CreateDb(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<OperationalDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options);

    private static async Task<Guid> SeedAsync(OperationalDbContext db, ValidationStatus status)
    {
        var execution = new ValidationExecution
        {
            Id = Guid.NewGuid(),
            CorrelationId = "unit",
            Status = status,
            SubmittedByUserId = "unit-user",
            OriginalFileId = Guid.NewGuid(),
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        db.ValidationExecutions.Add(execution);
        await db.SaveChangesAsync();
        return execution.Id;
    }
}
