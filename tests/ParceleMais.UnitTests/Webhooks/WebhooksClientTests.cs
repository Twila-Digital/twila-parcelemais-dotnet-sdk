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

    [Fact]
    public async Task ListAuditAsync_MontaAQueryStringComOsFiltrosInformados()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""
                { "itens": [], "pagina": { "tem_proximo": false, "tem_anterior": false, "numero": 1, "tamanho": 10, "total": 0 } }
                """));
        });

        var client = CreateClient(inner);
        var orderId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        await client.ListAuditAsync(new ListWebhookAuditRequest(
            StartDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(-3)),
            OrderId: orderId,
            OrderNumber: 1001,
            StatusCode: 500,
            Page: 2,
            PageSize: 20));

        Assert.NotNull(capturedUri);
        Assert.EndsWith("v1/webhooks/auditoria", capturedUri!.AbsolutePath);
        var query = Uri.UnescapeDataString(capturedUri.Query);
        Assert.Contains("dataInicio=2025-01-01T00:00:00.0000000-03:00", query);
        Assert.Contains($"pedidoId={orderId}", query);
        Assert.Contains("numeroPedido=1001", query);
        Assert.Contains("statusCode=500", query);
        Assert.Contains("pagina=2", query);
        Assert.Contains("tamanhoPagina=20", query);
        Assert.DoesNotContain("dataFim", query);
    }

    [Fact]
    public async Task ListAuditAsync_OffsetPositivo_CodificaOMaisNaQueryString()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""
                { "itens": [], "pagina": { "tem_proximo": false, "tem_anterior": false, "numero": 1, "tamanho": 10, "total": 0 } }
                """));
        });

        var client = CreateClient(inner);

        await client.ListAuditAsync(new ListWebhookAuditRequest(
            EndDate: new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.FromHours(2))));

        var rawQuery = capturedUri!.Query;
        Assert.Contains("dataFim=2025-12-31T23%3A59%3A59.0000000%2B02%3A00", rawQuery);
        Assert.DoesNotContain("+", rawQuery);
    }

    [Fact]
    public async Task ListAuditAsync_SemRequest_EnviaSoOsPadroesDePaginacao()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""
                { "itens": [], "pagina": { "tem_proximo": false, "tem_anterior": false, "numero": 1, "tamanho": 10, "total": 0 } }
                """));
        });

        var client = CreateClient(inner);

        await client.ListAuditAsync();

        Assert.Equal("?pagina=1&tamanhoPagina=10", capturedUri!.Query);
    }

    [Fact]
    public async Task ListAuditAsync_MapeiaOsItensEAPaginacao()
    {
        const string json = """
            {
                "itens": [
                    {
                        "id": "0b8f2c1e-4d3a-4f5b-9c6d-7e8f9a0b1c2d",
                        "tipo": 3,
                        "requisicao": "{\"pedidoId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"}",
                        "resposta": "Internal Server Error",
                        "statusCode": 500,
                        "dataCriacao": "2025-06-10T14:30:00-03:00"
                    }
                ],
                "pagina": { "tem_proximo": true, "tem_anterior": false, "numero": 1, "tamanho": 10, "total": 25 }
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var page = await client.ListAuditAsync();

        var audit = Assert.Single(page.Items);
        Assert.Equal(Guid.Parse("0b8f2c1e-4d3a-4f5b-9c6d-7e8f9a0b1c2d"), audit.Id);
        Assert.Equal(WebHookType.Order, audit.Type);
        Assert.Equal("""{"pedidoId":"3fa85f64-5717-4562-b3fc-2c963f66afa6"}""", audit.Request);
        Assert.Equal("Internal Server Error", audit.Response);
        Assert.Equal(500, audit.StatusCode);
        Assert.Equal(new DateTimeOffset(2025, 6, 10, 14, 30, 0, TimeSpan.FromHours(-3)), audit.CreatedAt);

        Assert.True(page.HasNext);
        Assert.False(page.HasPrevious);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
    }
}
