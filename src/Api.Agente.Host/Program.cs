using Api.Agente.Application.DependencyInjection;
using Api.Agente.Host.Auth;
using Api.Agente.Host.Endpoints;
using Api.Agente.Host.Health;
using Api.Agente.Host.Logging;
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

    builder.Services.AddAgenteOpenApiDocuments();
    builder.Services.AddProblemDetails();

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

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExecutionIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.MapAgenteOpenApiUi();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Ok(new { service = "Api.Agente", status = "ok" }))
        .WithName("Root");

    app.MapValidationEndpoints();

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
