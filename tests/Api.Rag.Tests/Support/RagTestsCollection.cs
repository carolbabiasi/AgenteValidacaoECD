namespace Api.Rag.Tests.Support;

[CollectionDefinition(Name)]
public sealed class RagTestsCollection : ICollectionFixture<RagWebApplicationFactory>
{
    public const string Name = "RagHostTests";
}
