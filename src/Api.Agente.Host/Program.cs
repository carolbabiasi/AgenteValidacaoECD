using Api.Agente.Application.DependencyInjection;
using Api.Agente.Host.Auth;
using Api.Agente.Host.Hosting;
using Api.Agente.Host.Endpoints;
using Api.Agente.Host.Health;
using Api.Agente.Host.Logging;
using Api.Agente.Host.Observability;
using Api.Agente.Host.OpenApi;
using Api.Agente.Host.Middleware;
using Api.Agente.Infrastructure.DependencyInjection;
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
    builder.Services.Configure<RagServiceOptions>(builder.Configuration.GetSection(RagServiceOptions.SectionName));
    builder.Services.AddHttpClient(nameof(RagMcpHealthCheck));

    builder.Services.AddAgenteApplication();
    builder.Services.AddAgenteInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddAgenteHomologAuth(builder.Configuration);
    builder.Services.AddAgenteTelemetry(builder.Configuration);

    builder.Services.AddAgenteOpenApiDocuments();
    builder.Services.AddProblemDetails();

    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (corsOrigins.Length > 0)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("SofiaBrowser", policy =>
                policy.WithOrigins(corsOrigins)
                    .WithMethods("GET", "POST", "OPTIONS")
                    .WithHeaders("Content-Type", "Authorization", SofiaA2aAuthorization.ServiceKeyHeader));
        });
    }

    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("Operational")!,
            name: "sqlserver-operational",
            tags: ["ready"])
        .AddNpgSql(
            builder.Configuration.GetConnectionString("Metrics")!,
            name: "postgres-metrics",
            tags: ["ready"])
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
            name: "redis",
            tags: ["ready"])
        .AddCheck<RagMcpHealthCheck>("rag-mcp", tags: ["ready"]);

    var app = builder.Build();
    app.UsePlatformHosting();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExecutionIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.MapAgenteOpenApiUi();

    if (VercelHostingExtensions.ShouldUseHttpsRedirection(app.Environment))
    {
        app.UseHttpsRedirection();
    }

    if (corsOrigins.Length > 0)
    {
        app.UseCors("SofiaBrowser");
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Ok(new { service = "Api.Agente", status = "ok" }))
        .WithName("Root");

    app.MapValidationEndpoints();
    app.MapSofiaA2aEndpoints();

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
    Log.Fatal(ex, "Api.Agente encerrada por falha na inicialização");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
