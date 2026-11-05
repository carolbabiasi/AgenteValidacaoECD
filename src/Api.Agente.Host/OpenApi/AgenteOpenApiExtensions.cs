using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Api.Agente.Host.OpenApi;

public static class AgenteOpenApiExtensions
{
    private const string DocumentName = "v1";

    public static IServiceCollection AddAgenteOpenApiDocuments(this IServiceCollection services)
    {
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Agente Validação ECD — REST API",
                    Version = DocumentName,
                    Description =
                        "Contrato V1: validação ECD, relatório JSON, histórico, exportação tabular e chat pós-validação. " +
                        "Referência: specs/001-validacao-ecd/contracts/api-agente-rest.md",
                };

                ApplyBearerSecurity(document);
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapAgenteOpenApiUi(this WebApplication app)
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
            options.WithTitle("Agente Validação ECD");
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
            Description =
                "JWT Bearer (homolog: issuer AgenteValidacaoECD-Dev; produção: Entra ID). " +
                "Roles: Validator, Admin.",
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
