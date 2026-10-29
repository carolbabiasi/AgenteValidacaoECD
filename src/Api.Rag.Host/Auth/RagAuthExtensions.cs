using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Rag.Host.Auth;

public static class RagAuthExtensions
{
    public const string RagAdminPolicy = "RagAdmin";
    public const string RagReaderPolicy = "RagReader";

    public static IServiceCollection AddRagHomologAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Auth:Jwt");
        var signingKey = section["SigningKey"]
            ?? "DEV-ONLY-ECD-RAG-SIGNING-KEY-MIN-32-CHARS!!!!";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = section["Issuer"] ?? "AgenteValidacaoECD-Rag-Dev",
                    ValidAudience = section["Audience"] ?? "AgenteValidacaoECD-Rag-Dev",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    RoleClaimType = ClaimTypes.Role,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(RagAdminPolicy, policy =>
                policy.RequireRole("RagAdmin"))
            .AddPolicy(RagReaderPolicy, policy =>
                policy.RequireRole("RagReader", "RagAdmin"));

        return services;
    }
}
