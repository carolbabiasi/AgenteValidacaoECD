using Api.Rag.Application.DependencyInjection;
using Api.Rag.Host.Logging;
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

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.With(new ExecutionIdEnricher(services.GetRequiredService<IHttpContextAccessor>()))
        .WriteTo.Console());

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddRagApplication();
    builder.Services.AddRagInfrastructure(builder.Configuration);

    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("Rag")!,
            name: "postgres-rag",
            tags: ["ready"]);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExecutionIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/", () => Results.Ok(new { service = "Api.Rag", status = "ok" }))
        .WithName("Root");

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
