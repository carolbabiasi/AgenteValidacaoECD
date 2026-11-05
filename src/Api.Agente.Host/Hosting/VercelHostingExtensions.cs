using Microsoft.AspNetCore.HttpOverrides;

namespace Api.Agente.Host.Hosting;

public static class VercelHostingExtensions
{
    public static bool IsVercel =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VERCEL"));

    public static WebApplicationBuilder ConfigurePlatformHosting(this WebApplicationBuilder builder)
    {
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

    public static bool ShouldUseHttpsRedirection(IHostEnvironment environment) =>
        !environment.IsEnvironment("Testing") && !IsVercel;
}
