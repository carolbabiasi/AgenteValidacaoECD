using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;

namespace Api.Rag.Host.Mcp;

public static class RagMcpServerExtensions
{
    public const string McpEndpointPath = "/mcp";

    public static IServiceCollection AddRagMcpServer(this IServiceCollection services)
    {
        services.AddMcpServer(options =>
            {
                options.ServerInfo = new()
                {
                    Name = "Api.Rag",
                    Version = "1.0.0",
                };
            })
            .WithHttpTransport()
            .AddAuthorizationFilters()
            .WithTools<RagMcpTools>();

        return services;
    }

    public static IEndpointConventionBuilder MapRagMcp(this WebApplication app)
    {
        return app.MapMcp(McpEndpointPath)
            .RequireAuthorization();
    }
}
