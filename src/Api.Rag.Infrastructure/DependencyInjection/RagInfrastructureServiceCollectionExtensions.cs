using Api.Rag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Infrastructure.DependencyInjection;

public static class RagInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRagInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<RagDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Rag")
                ?? throw new InvalidOperationException("Connection string 'Rag' não configurada.");
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__RagMigrationsHistory", "rag");
                npgsql.UseVector();
            });
        });

        return services;
    }
}
