namespace Api.Agente.Host.Middleware;

public sealed class ExecutionIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var executionId = context.Request.Headers["X-Execution-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(executionId))
        {
            executionId = Guid.NewGuid().ToString("N");
        }

        context.Items["ExecutionId"] = executionId;
        context.Response.Headers["X-Execution-Id"] = executionId;

        await next(context);
    }
}
