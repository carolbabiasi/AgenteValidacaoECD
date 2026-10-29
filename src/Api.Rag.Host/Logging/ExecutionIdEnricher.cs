using Serilog.Core;
using Serilog.Events;

namespace Api.Rag.Host.Logging;

public sealed class ExecutionIdEnricher(IHttpContextAccessor httpContextAccessor) : ILogEventEnricher
{
    public const string PropertyName = "ExecutionId";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (httpContext.Items.TryGetValue(PropertyName, out var item) && item is string executionId)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, executionId));
            return;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Execution-Id", out var header) &&
            !string.IsNullOrWhiteSpace(header))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(PropertyName, header.ToString()));
        }
    }
}
