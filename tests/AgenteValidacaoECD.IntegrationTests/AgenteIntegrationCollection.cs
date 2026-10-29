using Api.Agente.Tests.Support;

namespace AgenteValidacaoECD.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class AgenteIntegrationCollection : ICollectionFixture<AgenteWebApplicationFactory>
{
    public const string Name = "AgenteIntegration";
}
