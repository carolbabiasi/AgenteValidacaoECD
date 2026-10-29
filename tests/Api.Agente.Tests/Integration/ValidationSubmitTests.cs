using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Api.Agente.Tests.Support;

namespace Api.Agente.Tests.Integration;

[Collection(AgenteTestsCollection.Name)]
public sealed class ValidationSubmitTests
{
    private readonly AgenteWebApplicationFactory _factory;

    public ValidationSubmitTests(AgenteWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_RejectsNonTxtExtension()
    {
        using var client = CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("x")), "file", "ecd.pdf");

        var response = await client.PostAsync("/api/v1/validations", form);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", DevJwtTokenFactory.CreateValidatorToken());
        return client;
    }
}
