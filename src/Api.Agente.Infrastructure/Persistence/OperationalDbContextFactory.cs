using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Agente.Infrastructure.Persistence;

public sealed class OperationalDbContextFactory : IDesignTimeDbContextFactory<OperationalDbContext>
{
    public OperationalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OperationalDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=ecd_operational;User Id=sa;Password=Ecd_Dev_Passw0rd!;TrustServerCertificate=True",
            sql => sql.MigrationsHistoryTable("__OperationalMigrationsHistory", "operational"));

        return new OperationalDbContext(optionsBuilder.Options);
    }
}
