using Api.Agente.Application.Ecd;

namespace Api.Agente.Tests.Unit;

public sealed class EcdDisplayTextTests
{
    [Fact]
    public void FormatCnpj_AddsMask()
    {
        Assert.Equal("12.345.678/0001-95", EcdDisplayText.FormatCnpj("12345678000195"));
    }
}
