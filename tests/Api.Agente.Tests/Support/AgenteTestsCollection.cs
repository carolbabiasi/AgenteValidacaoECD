namespace Api.Agente.Tests.Support;

[CollectionDefinition(Name)]
public sealed class AgenteTestsCollection : ICollectionFixture<AgenteWebApplicationFactory>
{
    public const string Name = "AgenteHostTests";
}
