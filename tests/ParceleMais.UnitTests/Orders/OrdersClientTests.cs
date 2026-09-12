using System.Net;
using System.Net.Http.Json;
using ParceleMais.Errors;
using ParceleMais.Orders;
using ParceleMais.Orders.Models;
using ParceleMais.Serialization;
using ParceleMais.UnitTests.TestUtilities;

namespace ParceleMais.UnitTests.Orders;

public class OrdersClientTests
{
    private static IOrdersClient CreateClient(FakeHttpMessageHandler inner) =>
        new OrdersClient(TestApiClientFactory.CreateApiHttpClient(inner));

    [Fact]
    public async Task GetAsync_DesserializaOPedidoRealDoExemploDaApi()
    {
        const string json = """
            {
                "id": "3c90c3cc-0d44-4b50-8888-8dd25736052a",
                "numero": 1001,
                "status": { "valor": 2, "descricao": "Aprovado" },
                "total": 1500.00,
                "nomeCliente": "João da Silva",
                "documentoCliente": "12345678901",
                "prazo": 12,
                "razaoSocialEstabelecimento": "Loja Exemplo LTDA",
                "documentoEstabelecimento": "12345678000195",
                "descricao": "Compra de eletrônicos",
                "valorAprovado": 1500.00,
                "desembolsado": true,
                "criadoEm": "2025-01-15T10:30:00-03:00",
                "desembolsadoEm": "2025-01-16T09:00:00-03:00",
                "valorSolicitado": 1500.00
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var order = await client.GetAsync(Guid.Parse("3c90c3cc-0d44-4b50-8888-8dd25736052a"));

        Assert.Equal(1001, order.Number);
        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.Equal("Aprovado", order.StatusDescription);
        Assert.Equal(1500.00m, order.Total);
        Assert.Equal(12, order.Term);
        Assert.True(order.Disbursed);
    }

    [Fact]
    public async Task GetAsync_ComStatusDesconhecido_MapeiaParaUnknown_SemLancar()
    {
        const string json = """
            {
                "id": "3c90c3cc-0d44-4b50-8888-8dd25736052a",
                "numero": 1001,
                "status": { "valor": 99, "descricao": "StatusFuturo" },
                "documentoCliente": "12345678901",
                "razaoSocialEstabelecimento": "Loja Exemplo LTDA",
                "documentoEstabelecimento": "12345678000195",
                "criadoEm": "2025-01-15T10:30:00-03:00"
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var order = await client.GetAsync(Guid.NewGuid());

        Assert.Equal(OrderStatus.Unknown, order.Status);
        Assert.Equal("StatusFuturo", order.StatusDescription);
    }

    [Fact]
    public async Task CreateAsync_EnviaOPayloadCorretoEAIdempotencyKey()
    {
        HttpRequestMessage? capturedRequest = null;
        var inner = new FakeHttpMessageHandler(async (request, _, _) =>
        {
            capturedRequest = request;
            return JsonResponse("""{ "pedidoId": "3c90c3cc-0d44-4b50-8888-8dd25736052a" }""");
        });

        var client = CreateClient(inner);

        var request = new CreateOrderRequest(
            "12345678901",
            "+55 (11) 91234-5678",
            "12345678000195",
            1500.00m,
            "João da Silva",
            "joao.silva@email.com",
            new DateTimeOffset(1990, 5, 15, 0, 0, 0, TimeSpan.Zero),
            new Address("Av. Paulista", "1578", "Bela Vista", "São Paulo", "SP", "01311000", "Bloco B, Apto 1203"));

        var orderId = await client.CreateAsync(request);

        Assert.Equal(Guid.Parse("3c90c3cc-0d44-4b50-8888-8dd25736052a"), orderId);
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("Idempotency-Key"));

        var body = await capturedRequest.Content!.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>(ParceleMaisJsonOptions.Default);
        Assert.Equal("12345678901", body!["cpf"].GetString());
        Assert.Equal(1500.00m, body["valorSolicitado"].GetDecimal());
    }

    [Fact]
    public async Task ListAsync_MontaAQueryStringComOsFiltrosInformados()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""
                { "items": [], "pagina": { "tem_proximo": false, "tem_anterior": false, "numero": 1, "tamanho": 10, "total": 0 } }
                """));
        });

        var client = CreateClient(inner);

        await client.ListAsync(new ListOrdersRequest(Status: OrderStatus.Approved, CustomerDocument: "12345678901", Page: 2, PageSize: 20));

        Assert.NotNull(capturedUri);
        var query = capturedUri!.Query;
        Assert.Contains("status=2", query);
        Assert.Contains("documentoCliente=12345678901", query);
        Assert.Contains("pagina=2", query);
        Assert.Contains("tamanhoPagina=20", query);
    }

    [Fact]
    public async Task StartCdcSaleAsync_RetornaOLinkDeCheckout()
    {
        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(
            """{ "linkPagamento": "https://pagamento.parcelemais.com.br/checkout/3c90c3cc-0d44-4b50-8888-8dd25736052a" }""")));

        var client = CreateClient(inner);
        var link = await client.StartCdcSaleAsync(Guid.NewGuid());

        Assert.Equal("https://pagamento.parcelemais.com.br/checkout/3c90c3cc-0d44-4b50-8888-8dd25736052a", link.Url);
    }

    [Fact]
    public async Task ImportInvoiceAsync_EnviaOArquivoEmBase64_ENaoLancaEm204()
    {
        HttpRequestMessage? capturedRequest = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(inner);
        var file = InvoiceFile.FromBytes([1, 2, 3, 4], "nota-fiscal.pdf");

        await client.ImportInvoiceAsync(Guid.NewGuid(), file);

        var body = await capturedRequest!.Content!.ReadFromJsonAsync<Dictionary<string, System.Text.Json.JsonElement>>(ParceleMaisJsonOptions.Default);
        Assert.Equal(Convert.ToBase64String([1, 2, 3, 4]), body!["arquivoBase64"].GetString());
        Assert.Equal("nota-fiscal.pdf", body["nomeArquivo"].GetString());
    }

    [Fact]
    public async Task GetAsync_Com404_LancaParceleMaisApiException()
    {
        const string json = """
            {
                "tipo": "Order.NotFound",
                "titulo": "Não encontrado",
                "status": 404,
                "detalhe": "Pedido não encontrado."
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json, HttpStatusCode.NotFound)));
        var client = CreateClient(inner);

        var exception = await Assert.ThrowsAsync<ParceleMaisApiException>(() => client.GetAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal("Order.NotFound", exception.ErrorCode);
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(System.Text.Json.JsonDocument.Parse(json).RootElement) };
}
