using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Api.Agente.Tests.Support;

public sealed class AgenteWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:UseInMemoryDatabase"] = "true",
                ["Testing:InMemoryDatabaseName"] = Guid.NewGuid().ToString("N"),
            });
        });
    }
}
