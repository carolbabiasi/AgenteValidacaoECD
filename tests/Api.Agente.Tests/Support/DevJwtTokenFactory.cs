using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using Microsoft.IdentityModel.Tokens;

namespace Api.Agente.Tests.Support;

public static class DevJwtTokenFactory
{
    public static string CreateValidatorToken(string userId = "test-user") =>
        CreateToken(userId, "Validator");

    public static string CreateToken(string userId, params string[] roles)
    {
        const string signingKey = "DEV-ONLY-ECD-AGENTE-SIGNING-KEY-MIN-32-CHARS!!";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: "AgenteValidacaoECD-Dev",
            audience: "AgenteValidacaoECD-Dev",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
