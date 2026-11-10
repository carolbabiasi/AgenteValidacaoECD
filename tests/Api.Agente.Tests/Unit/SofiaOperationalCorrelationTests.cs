using Api.Agente.Application.Sofia;

namespace Api.Agente.Tests.Unit;

public sealed class SofiaOperationalCorrelationTests
{
    [Fact]
    public void ForDocument_ShortIdDocumento_FitsMaxLength()
    {
        var id = Guid.Parse("e1b3a4cb-d729-4bd4-b796-b9c94be07b46");
        var correlation = SofiaOperationalCorrelation.ForDocument(id, "ERRO_04_CONTA_INEXISTENTE");

        Assert.True(correlation.Length <= SofiaOperationalCorrelation.MaxLength);
        Assert.Equal($"{id:N}:ERRO_04_CONTA_INEXISTENTE", correlation);
    }

    [Fact]
    public void ForDocument_UuidIdDocumento_DoesNotExceedDatabaseLimit()
    {
        var id = Guid.Parse("e1b3a4cb-d729-4bd4-b796-b9c94be07b46");
        var docId = "8b2f0ad6-c2e4-4a68-8bdd-20f785bc798e";
        var correlation = SofiaOperationalCorrelation.ForDocument(id, docId);

        Assert.True(correlation.Length <= SofiaOperationalCorrelation.MaxLength);
        Assert.StartsWith($"{id:N}:", correlation);
        Assert.NotEqual($"{id:N}:{docId}", correlation);
    }
}
