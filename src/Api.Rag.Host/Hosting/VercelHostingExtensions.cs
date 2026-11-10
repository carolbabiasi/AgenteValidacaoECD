using Microsoft.AspNetCore.HttpOverrides;

namespace Api.Rag.Host.Hosting;

/// <summary>
/// Espelha o helper de mesmo nome do Api.Agente.Host. É duplicado de propósito:
/// os dois hosts não compartilham projeto (constitution P7/AR-01), então uma
/// biblioteca comum entre eles atravessaria a fronteira dos serviços.
/// </summary>
public static class VercelHostingExtensions
{
    public static bool IsVercel =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VERCEL"));

    public static WebApplicationBuilder ConfigurePlatformHosting(this WebApplicationBuilder builder)
    {
        // A Vercel roteia para a porta de $PORT; sem isto o host fica ouvindo
        // 8080 e a requisição nunca chega (FUNCTION_INVOCATION_FAILED).
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
        {
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        }

        if (IsVercel)
        {
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        return builder;
    }

    public static WebApplication UsePlatformHosting(this WebApplication app)
    {
        if (IsVercel)
        {
            app.UseForwardedHeaders();
        }

        return app;
    }

    // Na Vercel o proxy entrega HTTP ao container: redirecionar para HTTPS vira laço.
    public static bool ShouldUseHttpsRedirection(IHostEnvironment environment) =>
        !environment.IsEnvironment("Testing") && !IsVercel;
}
