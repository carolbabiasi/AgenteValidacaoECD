using Api.Agente.Application.Sofia;
using Api.Agente.Domain.Sofia;

namespace Api.Agente.Tests.Unit;

public sealed class SofiaContractValidatorTests
{
    [Fact]
    public void Validate_AcceptsMinimalValidRequest()
    {
        var solicitacao = MinimalRequest();
        var errors = SofiaContractValidator.Validate(solicitacao);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RejectsMissingDocumentText()
    {
        var solicitacao = MinimalRequest();
        solicitacao.Documentos[0].Texto = "";
        var errors = SofiaContractValidator.Validate(solicitacao);
        Assert.Contains(errors, e => e.Contains("texto", StringComparison.Ordinal));
    }

    private static SolicitacaoAnaliseSofia MinimalRequest() =>
        new()
        {
            VersaoContrato = SofiaContractValidator.ContractVersion,
            IdExecucao = Guid.NewGuid(),
            IdSolicitacao = Guid.NewGuid(),
            Usuario = new SofiaUsuario { Nome = "Teste SOFIA" },
            Pergunta = "Validar ECD",
            Documentos =
            [
                new SofiaDocumento
                {
                    IdDocumento = "doc-001",
                    Nome = "ecd.txt",
                    Texto = "|0000|LECD|9.0|01012025|31122025|EMP|\r\n|0990|2|\r\n",
                },
            ],
        };
}
