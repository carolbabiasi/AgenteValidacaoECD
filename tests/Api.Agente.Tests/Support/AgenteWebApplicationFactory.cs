using Api.Agente.Domain.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                ["SOFIA_A2A_TOKEN"] = Integration.SofiaA2aContractTests.TestA2aToken,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IRagMcpClient>();
            services.AddSingleton<IRagMcpClient, FakeRagMcpClient>();

            services.RemoveAll<IChatSessionStore>();
            services.AddSingleton<IChatSessionStore, Api.Agente.Infrastructure.Chat.InMemoryChatSessionStore>();
        });
    }
}
