using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Agente.Infrastructure.Mcp;

public sealed class RagServiceJwtTokenFactory(IOptions<RagMcpOptions> options)
{
    public string CreateReaderToken(TimeSpan? lifetime = null)
    {
        var jwt = options.Value.Jwt;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "agente-service"),
                new Claim(ClaimTypes.Role, "RagReader"),
            ],
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
