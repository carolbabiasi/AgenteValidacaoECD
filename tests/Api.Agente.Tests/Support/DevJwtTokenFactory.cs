using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Agente.Tests.Support;

public static class DevJwtTokenFactory
{
    public static string CreateValidatorToken()
    {
        const string signingKey = "DEV-ONLY-ECD-AGENTE-SIGNING-KEY-MIN-32-CHARS!!";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "AgenteValidacaoECD-Dev",
            audience: "AgenteValidacaoECD-Dev",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Role, "Validator"),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
