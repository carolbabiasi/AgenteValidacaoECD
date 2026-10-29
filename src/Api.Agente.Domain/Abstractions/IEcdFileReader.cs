using Api.Agente.Domain.Ecd;

namespace Api.Agente.Domain.Abstractions;

public interface IEcdFileReader
{
    Task<IReadOnlyList<EcdLine>> ReadAllLinesAsync(Stream content, CancellationToken cancellationToken = default);
}
