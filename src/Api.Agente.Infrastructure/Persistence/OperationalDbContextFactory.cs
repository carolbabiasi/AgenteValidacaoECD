using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Agente.Infrastructure.Persistence;

public sealed class OperationalDbContextFactory : IDesignTimeDbContextFactory<OperationalDbContext>
{
    public OperationalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OperationalDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=ecd;Username=ecd;Password=ecd_dev",
            npgsql => npgsql.MigrationsHistoryTable("__OperationalMigrationsHistory", "operational"));

        return new OperationalDbContext(optionsBuilder.Options);
    }
}
