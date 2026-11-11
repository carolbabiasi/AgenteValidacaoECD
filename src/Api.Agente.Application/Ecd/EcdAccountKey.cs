namespace Api.Agente.Application.Ecd;

public readonly record struct EcdAccountKey(string CodConta, string CodCcus)
{
    public static EcdAccountKey Create(string? codConta, string? codCcus) =>
        new(codConta?.Trim() ?? string.Empty, codCcus?.Trim() ?? string.Empty);
}
