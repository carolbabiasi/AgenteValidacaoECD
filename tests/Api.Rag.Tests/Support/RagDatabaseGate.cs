using Api.Rag.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Rag.Tests.Support;

/// <summary>
/// Portao dos testes de integracao do RAG, que exigem PostgreSQL real.
/// Sem banco acessivel o teste e ignorado, para nao travar quem desenvolve sem
/// Docker. Em ambiente que exige banco (CI), falhar e obrigatorio: um teste que
/// retorna cedo passa sem asserção nenhuma e a suite fica verde enganosa.
/// </summary>
public static class RagDatabaseGate
{
    /// <summary>Variavel que transforma "banco ausente" em falha. O workflow de CI a define.</summary>
    public const string RequirePostgresVariable = "ECD_REQUIRE_POSTGRES";

    public static async Task<bool> TryMigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<RagDbContext>();
            await db.Database.MigrateAsync(cancellationToken);

            if (await db.Database.CanConnectAsync(cancellationToken))
            {
                return true;
            }

            return HandleUnavailable(null);
        }
        catch (Exception ex)
        {
            return HandleUnavailable(ex);
        }
    }

    private static bool HandleUnavailable(Exception? cause)
    {
        if (IsPostgresRequired())
        {
            throw new InvalidOperationException(
                $"PostgreSQL indisponivel para os testes de integracao do RAG e {RequirePostgresVariable} esta ligado. " +
                "Este teste nao pode ser ignorado em silencio neste ambiente.",
                cause);
        }

        Console.WriteLine(
            $"[RagDatabaseGate] PostgreSQL indisponivel; teste ignorado SEM asserção. " +
            $"Defina {RequirePostgresVariable}=true para falhar em vez de ignorar. " +
            $"Causa: {cause?.Message ?? "CanConnect retornou false"}");

        return false;
    }

    private static bool IsPostgresRequired()
    {
        var value = Environment.GetEnvironmentVariable(RequirePostgresVariable);

        return !string.IsNullOrWhiteSpace(value)
            && (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
    }
}
