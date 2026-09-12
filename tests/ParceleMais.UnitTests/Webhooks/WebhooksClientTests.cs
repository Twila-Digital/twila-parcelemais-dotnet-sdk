using System.Net;
using System.Net.Http.Json;
using ParceleMais.UnitTests.TestUtilities;
using ParceleMais.Webhooks;
using ParceleMais.Webhooks.Models;

namespace ParceleMais.UnitTests.Webhooks;

public class WebhooksClientTests
{
    private static IWebhooksClient CreateClient(FakeHttpMessageHandler inner) =>
        new WebhooksClient(TestApiClientFactory.CreateApiHttpClient(inner));

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(System.Text.Json.JsonDocument.Parse(json).RootElement) };

    [Fact]
    public async Task CreateAsync_EnviaOPayloadCorretoEExpoeOSigningSecret()
    {
        HttpRequestMessage? capturedRequest = null;
        var inner = new FakeHttpMessageHandler(async (request, _, _) =>
        {
            capturedRequest = request;
            return JsonResponse("""{ "chaveAssinatura": "3f2e1a9c8b7d6e5f4a3b2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6a5b4c3d2e1f" }""");
        });

        var client = CreateClient(inner);
        var result = await client.CreateAsync(new CreateWebhookRequest(WebHookType.Order, "https://parceiro.exemplo.com/webhooks/pedidos", WebHookAuthenticationType.Jwt, "seu-token-jwt"));

        Assert.Equal("3f2e1a9c8b7d6e5f4a3b2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6a5b4c3d2e1f", result.SigningSecret);

        var body = await capturedRequest!.Content!.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>();
        Assert.Equal(3, body!["tipo"].GetInt32());
        Assert.Equal("https://parceiro.exemplo.com/webhooks/pedidos", body["url"].GetString());
        Assert.Equal(3, body["tipoAutenticacao"].GetInt32());
        Assert.Equal("seu-token-jwt", body["credencial"].GetString());
    }

    [Fact]
    public async Task ListAsync_DesserializaOArrayDiretoSemWrapperDePaginacao()
    {
        const string json = """
            [
                { "tipo": 3, "url": "https://parceiro.exemplo.com/webhooks/pedidos", "tipoAutenticacao": 3 }
            ]
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var webhooks = await client.ListAsync();

        Assert.Single(webhooks);
        Assert.Equal(WebHookType.Order, webhooks[0].Type);
        Assert.Equal(WebHookAuthenticationType.Jwt, webhooks[0].AuthenticationType);
    }

    [Fact]
    public async Task ListAsync_ComTipoDesconhecido_MapeiaParaUnknown_SemLancar()
    {
        const string json = """[{ "tipo": 99, "url": "https://parceiro.exemplo.com/x", "tipoAutenticacao": 99 }]""";

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var webhooks = await client.ListAsync();

        Assert.Equal(WebHookType.Unknown, webhooks[0].Type);
        Assert.Equal(WebHookAuthenticationType.Unknown, webhooks[0].AuthenticationType);
    }

    [Fact]
    public async Task UpdateAsync_EnviaParaOPathComOTipoNaUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = CreateClient(inner);
        await client.UpdateAsync(WebHookType.Order, new UpdateWebhookRequest("https://parceiro.exemplo.com/webhooks/pedidos", WebHookAuthenticationType.Basic, "usuario:senha"));

        Assert.Equal(HttpMethod.Put, capturedRequest!.Method);
        Assert.EndsWith("v1/webhooks/3", capturedRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteAsync_EnviaParaOPathComOTipoNaUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = CreateClient(inner);
        await client.DeleteAsync(WebHookType.Customer);

        Assert.Equal(HttpMethod.Delete, capturedRequest!.Method);
        Assert.EndsWith("v1/webhooks/1", capturedRequest.RequestUri!.AbsolutePath);
    }
}
