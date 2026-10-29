using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Rag.Tests.Support;

public static class RagDevJwtTokenFactory
{
    public static string CreateRagAdminToken()
    {
        const string signingKey = "DEV-ONLY-ECD-RAG-SIGNING-KEY-MIN-32-CHARS!!!!";
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "AgenteValidacaoECD-Rag-Dev",
            audience: "AgenteValidacaoECD-Rag-Dev",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "rag-test-admin"),
                new Claim(ClaimTypes.Role, "RagAdmin"),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
