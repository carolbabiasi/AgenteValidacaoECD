using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Agente.Host.Auth;

public static class AuthExtensions
{
    public const string ValidatorPolicy = "Validator";
    public const string AdminPolicy = "Admin";

    public static IServiceCollection AddAgenteHomologAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Auth:Jwt");
        var signingKey = section["SigningKey"]
            ?? "DEV-ONLY-ECD-AGENTE-SIGNING-KEY-MIN-32-CHARS!!";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = section["Issuer"] ?? "AgenteValidacaoECD-Dev",
                    ValidAudience = section["Audience"] ?? "AgenteValidacaoECD-Dev",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    RoleClaimType = ClaimTypes.Role,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(ValidatorPolicy, policy =>
                policy.RequireRole("Validator", "Admin"))
            .AddPolicy(AdminPolicy, policy =>
                policy.RequireRole("Admin"));

        return services;
    }
}
