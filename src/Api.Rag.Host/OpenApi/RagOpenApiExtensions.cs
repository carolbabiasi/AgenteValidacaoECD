using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Api.Rag.Host.OpenApi;

public static class RagOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddRagOpenApiDocuments(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Api.RAG — Administração e ingestão Manual ECD",
                    Version = DocumentName,
                    Description =
                        "REST admin (ingestão, versões de manual, chunks). Runtime do Agente usa MCP, não estes endpoints. " +
                        "Referência: specs/001-validacao-ecd/contracts/api-rag-rest.md",
                };

                ApplyBearerSecurity(document);
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapRagOpenApiUi(this WebApplication app)
    {
        if (!ShouldExpose(app.Environment, app.Configuration))
        {
            return app;
        }

        app.MapOpenApi("/openapi/{documentName}.json");

        app.MapGet("/swagger", () => Results.Redirect("/swagger/v1"))
            .ExcludeFromDescription();

        app.MapScalarApiReference("/swagger", options =>
        {
            options.WithTitle("Api.RAG — Manual ECD");
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
        });

        return app;
    }

    internal static bool ShouldExpose(IHostEnvironment environment, IConfiguration configuration) =>
        !environment.IsEnvironment("Testing") &&
        (environment.IsDevelopment() || configuration.GetValue("OpenApi:ExposeUi", false));

    private static void ApplyBearerSecurity(OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        const string schemeId = "Bearer";

        document.Components.SecuritySchemes[schemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer. Roles: RagAdmin (ingestão), RagReader (consulta).",
        };

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(schemeId, document)] = [],
            },
        ];
    }
}
