using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Api.Agente.Host.Health;

public sealed class RagServiceOptions
{
    public const string SectionName = "Rag";

    public Uri BaseUrl { get; set; } = new("http://localhost:5201");
}

public sealed class RagMcpHealthCheck(
    IHttpClientFactory httpClientFactory,
    IOptions<RagServiceOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(nameof(RagMcpHealthCheck));
        var uri = new Uri(options.Value.BaseUrl, "/health/ready");

        try
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Api.RAG respondeu em /health/ready.")
                : HealthCheckResult.Degraded($"Api.RAG retornou {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Api.RAG indisponível.", ex);
        }
    }
}
