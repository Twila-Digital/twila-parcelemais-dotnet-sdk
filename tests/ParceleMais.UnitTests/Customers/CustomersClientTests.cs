using System.Net;
using System.Net.Http.Json;
using ParceleMais.Customers;
using ParceleMais.Customers.Models;
using ParceleMais.Errors;
using ParceleMais.UnitTests.TestUtilities;

namespace ParceleMais.UnitTests.Customers;

public class CustomersClientTests
{
    private static ICustomersClient CreateClient(FakeHttpMessageHandler inner) =>
        new CustomersClient(TestApiClientFactory.CreateApiHttpClient(inner));

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(System.Text.Json.JsonDocument.Parse(json).RootElement) };

    [Fact]
    public async Task GetAsync_DesserializaOExemploRealDaApi()
    {
        const string json = """
            {
                "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
                "nome": "Maria Souza",
                "documento": "98765432100",
                "endereco": {
                    "rua": "Av. Paulista",
                    "cidade": "São Paulo",
                    "estado": "SP",
                    "bairro": "Bela Vista",
                    "cep": "01310-100",
                    "pais": "BR",
                    "numero": "1578",
                    "complemento": "Apto 42"
                },
                "dataDeNascimento": "1990-03-22T00:00:00Z",
                "email": "maria.souza@email.com",
                "celular": "+55 (11) 91234-5678"
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var customer = await client.GetAsync(Guid.Parse("9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"));

        Assert.Equal("Maria Souza", customer.Name);
        Assert.Equal("98765432100", customer.Document);
        Assert.Equal("Av. Paulista", customer.Address!.Street);
        Assert.Equal("SP", customer.Address.State);
        Assert.Equal("maria.souza@email.com", customer.Email);
    }

    [Fact]
    public async Task GetAsync_SemEndereco_NaoLanca()
    {
        const string json = """
            {
                "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
                "nome": "Maria Souza",
                "documento": "98765432100",
                "dataDeNascimento": "1990-03-22T00:00:00Z"
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var customer = await client.GetAsync(Guid.NewGuid());

        Assert.Null(customer.Address);
        Assert.Null(customer.Email);
    }

    [Fact]
    public async Task ListAsync_MontaAQueryStringComOsFiltrosInformados()
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
        await client.ListAsync(new ListCustomersRequest(Name: "Maria Souza", Document: "12345678901", Page: 2, PageSize: 20));

        Assert.NotNull(capturedUri);
        var query = capturedUri!.Query;
        Assert.Contains("nome=Maria", query);
        Assert.Contains("documento=12345678901", query);
        Assert.Contains("pagina=2", query);
        Assert.Contains("tamanhoPagina=20", query);
    }

    [Fact]
    public async Task ListAsync_DesserializaAPaginacaoCorretamente()
    {
        const string json = """
            {
                "itens": [
                    { "id": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d", "nome": "Maria Souza", "documento": "98765432100", "dataDeNascimento": "1990-03-22T00:00:00Z" }
                ],
                "pagina": { "tem_proximo": true, "tem_anterior": false, "numero": 2, "tamanho": 15, "total": 45 }
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var result = await client.ListAsync();

        Assert.Single(result.Items);
        Assert.True(result.HasNext);
        Assert.False(result.HasPrevious);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(45, result.TotalCount);
    }

    [Fact]
    public async Task GetAsync_Com404_LancaParceleMaisApiException()
    {
        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(
            """{ "tipo": "Customer.NotFound", "status": 404, "detalhe": "Cliente não encontrado." }""", HttpStatusCode.NotFound)));

        var client = CreateClient(inner);

        var exception = await Assert.ThrowsAsync<ParceleMaisApiException>(() => client.GetAsync(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
}
