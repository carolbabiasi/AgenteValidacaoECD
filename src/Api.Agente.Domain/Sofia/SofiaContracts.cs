using System.Text.Json.Serialization;

namespace Api.Agente.Domain.Sofia;

public sealed class SolicitacaoAnaliseSofia
{
    [JsonPropertyName("versaoContrato")]
    public string? VersaoContrato { get; set; }

    [JsonPropertyName("idExecucao")]
    public Guid IdExecucao { get; set; }

    [JsonPropertyName("idSolicitacao")]
    public Guid IdSolicitacao { get; set; }

    [JsonPropertyName("usuario")]
    public SofiaUsuario? Usuario { get; set; }

    [JsonPropertyName("pergunta")]
    public string? Pergunta { get; set; }

    [JsonPropertyName("documentos")]
    public List<SofiaDocumento> Documentos { get; set; } = [];

    [JsonPropertyName("contexto")]
    public SofiaContexto? Contexto { get; set; }
}

public sealed class SofiaUsuario
{
    [JsonPropertyName("nome")]
    public string? Nome { get; set; }
}

public sealed class SofiaDocumento
{
    [JsonPropertyName("idDocumento")]
    public string? IdDocumento { get; set; }

    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonPropertyName("texto")]
    public string? Texto { get; set; }
}

public sealed class SofiaContexto
{
    [JsonPropertyName("idioma")]
    public string? Idioma { get; set; }

    [JsonPropertyName("instrucoes")]
    public string? Instrucoes { get; set; }
}

public sealed class ResultadoAnaliseSofia
{
    [JsonPropertyName("versaoContrato")]
    public string VersaoContrato { get; set; } = "1.0";

    [JsonPropertyName("idExecucao")]
    public Guid IdExecucao { get; set; }

    [JsonPropertyName("idSolicitacao")]
    public Guid IdSolicitacao { get; set; }

    [JsonPropertyName("agente")]
    public SofiaAgenteInfo Agente { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "concluido";

    [JsonPropertyName("analise")]
    public string Analise { get; set; } = string.Empty;

    [JsonPropertyName("achados")]
    public List<SofiaAchado> Achados { get; set; } = [];

    [JsonPropertyName("observacoes")]
    public List<string> Observacoes { get; set; } = [];

    /// <summary>ID da execução no pipeline (relatório REST). Não exibir na UI conversacional.</summary>
    [JsonPropertyName("idValidacaoInterna")]
    public Guid? IdValidacaoInterna { get; set; }

    [JsonPropertyName("erro")]
    public SofiaErro? Erro { get; set; }
}

public sealed class SofiaAgenteInfo
{
    [JsonPropertyName("idAgente")]
    public string IdAgente { get; set; } = "agente-validacao-ecd";

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "Agente Validação ECD";
}

public sealed class SofiaAchado
{
    [JsonPropertyName("idAchado")]
    public string IdAchado { get; set; } = string.Empty;

    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [JsonPropertyName("idDocumentos")]
    public List<string> IdDocumentos { get; set; } = [];

    [JsonPropertyName("evidencias")]
    public List<string> Evidencias { get; set; } = [];

    [JsonPropertyName("severidade")]
    public string? Severidade { get; set; }
}

public sealed class SofiaErro
{
    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; set; } = string.Empty;
}
