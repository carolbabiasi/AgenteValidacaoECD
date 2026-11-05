using Api.Agente.Domain.Sofia;

namespace Api.Agente.Application.Sofia;

public static class SofiaContractValidator
{
    public const string ContractVersion = "1.0";

    public static IReadOnlyList<string> Validate(SolicitacaoAnaliseSofia? solicitacao)
    {
        var errors = new List<string>();
        if (solicitacao is null)
        {
            errors.Add("Payload SOFIA ausente.");
            return errors;
        }

        if (!string.Equals(solicitacao.VersaoContrato, ContractVersion, StringComparison.Ordinal))
        {
            errors.Add($"versaoContrato deve ser {ContractVersion}.");
        }

        if (solicitacao.IdExecucao == Guid.Empty)
        {
            errors.Add("idExecucao obrigatório.");
        }

        if (solicitacao.IdSolicitacao == Guid.Empty)
        {
            errors.Add("idSolicitacao obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(solicitacao.Usuario?.Nome))
        {
            errors.Add("usuario.nome obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(solicitacao.Pergunta))
        {
            errors.Add("pergunta obrigatória.");
        }

        if (solicitacao.Documentos.Count == 0)
        {
            errors.Add("documentos deve conter ao menos um item.");
        }

        for (var i = 0; i < solicitacao.Documentos.Count; i++)
        {
            var doc = solicitacao.Documentos[i];
            if (string.IsNullOrWhiteSpace(doc.IdDocumento))
            {
                errors.Add($"documentos[{i}].idDocumento obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(doc.Nome))
            {
                errors.Add($"documentos[{i}].nome obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(doc.Texto))
            {
                errors.Add($"documentos[{i}].texto obrigatório.");
            }
        }

        return errors;
    }
}
