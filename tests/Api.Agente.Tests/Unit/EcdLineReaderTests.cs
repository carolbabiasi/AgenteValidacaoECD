using Api.Agente.Infrastructure.Ecd;
using Api.Agente.Tests.Fixtures;

namespace Api.Agente.Tests.Unit;

public sealed class EcdLineReaderTests
{
    [Fact]
    public async Task ReadAllLinesAsync_ParsesRecordTypes()
    {
        var reader = new StreamingEcdFileReader();
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SampleEcdFiles.ValidMinimal));

        var lines = await reader.ReadAllLinesAsync(stream);

        Assert.Equal(2, lines.Count);
        Assert.Equal("0000", lines[0].RecordType);
        Assert.Equal("0990", lines[1].RecordType);
    }
}
