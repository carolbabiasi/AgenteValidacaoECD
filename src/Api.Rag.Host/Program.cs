using Api.Rag.Application.DependencyInjection;
using Api.Rag.Host.Auth;
using Api.Rag.Host.Endpoints;
using Api.Rag.Host.Hosting;
using Api.Rag.Host.Logging;
using Api.Rag.Host.Mcp;
using Api.Rag.Host.OpenApi;
using Api.Rag.Host.Middleware;
using Api.Rag.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigurePlatformHosting();

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.With(new ExecutionIdEnricher(services.GetRequiredService<IHttpContextAccessor>()))
        .WriteTo.Console());

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddRagApplication();
    builder.Services.AddRagInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddRagHomologAuth(builder.Configuration);
    builder.Services.AddRagMcpServer();

    builder.Services.AddRagOpenApiDocuments();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("Rag")!,
            name: "postgres-rag",
            tags: ["ready"]);

    var app = builder.Build();
    app.UsePlatformHosting();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExecutionIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.MapRagOpenApiUi();

    if (VercelHostingExtensions.ShouldUseHttpsRedirection(app.Environment))
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Ok(new { service = "Api.Rag", status = "ok" }))
        .WithName("Root");

    app.MapRagAdminEndpoints();
    app.MapRagMcp();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Api.Rag encerrada por falha na inicialização");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
