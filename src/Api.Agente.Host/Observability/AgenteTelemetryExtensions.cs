using Api.Agente.Application.Pipeline;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Api.Agente.Host.Observability;

public static class AgenteTelemetryExtensions
{
    public const string SectionName = "OpenTelemetry";

    public static IServiceCollection AddAgenteTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var serviceName = section["ServiceName"] ?? "Api.Agente";
        var otlpEndpoint = section["OtlpEndpoint"];
        var useConsoleExporter = section.GetValue<bool>("ConsoleExporter");

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(EcdPipelineTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }

                if (useConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            });

        return services;
    }
}
