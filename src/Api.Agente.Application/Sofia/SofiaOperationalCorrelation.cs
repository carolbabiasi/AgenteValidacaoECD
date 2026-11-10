using System.Security.Cryptography;
using System.Text;

namespace Api.Agente.Application.Sofia;

/// <summary>
/// CorrelationId persistido em <c>ValidationExecutions</c> (varchar 64).
/// </summary>
public static class SofiaOperationalCorrelation
{
    public const int MaxLength = 64;

    public static string ForDocument(Guid idSolicitacao, string idDocumento)
    {
        var solicitacao = idSolicitacao.ToString("N");
        var maxDocPart = MaxLength - solicitacao.Length - 1;
        if (maxDocPart < 8)
        {
            return solicitacao[..Math.Min(MaxLength, solicitacao.Length)];
        }

        var docPart = idDocumento.Trim();
        if (docPart.Length > maxDocPart)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(idDocumento));
            docPart = Convert.ToHexString(hash)[..maxDocPart].ToLowerInvariant();
        }

        return $"{solicitacao}:{docPart}";
    }
}
