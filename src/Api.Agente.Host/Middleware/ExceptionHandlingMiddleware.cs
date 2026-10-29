using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Agente.Host.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado na requisição {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno do servidor",
                Detail = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.Message
                    : "Ocorreu um erro inesperado.",
                Instance = context.Request.Path,
            };

            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            context.Response.StatusCode = problem.Status.Value;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
