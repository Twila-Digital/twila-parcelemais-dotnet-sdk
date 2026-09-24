using System.Net;
using System.Net.Http.Json;
using ParceleMais.Errors;
using ParceleMais.Establishments;
using ParceleMais.Establishments.Models;
using ParceleMais.UnitTests.TestUtilities;

namespace ParceleMais.UnitTests.Establishments;

public class EstablishmentsClientTests
{
    private static IEstablishmentsClient CreateClient(FakeHttpMessageHandler inner) =>
        new EstablishmentsClient(TestApiClientFactory.CreateApiHttpClient(inner));

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(System.Text.Json.JsonDocument.Parse(json).RootElement) };

    private static HttpResponseMessage EmptyResponse() => new(HttpStatusCode.OK);

    private static CreateEstablishmentRequest NewCreateRequest() => new(
        Document: "12345678000199",
        LegalName: "Loja Centro LTDA",
        TradeName: "Loja Centro",
        DisbursementModel: DisbursementModel.EstablishmentChain,
        Owner: new EstablishmentOwner("Maria Souza", "maria@loja.com.br", "+5511999998888"),
        BankAccount: new EstablishmentBankAccount("341", "1234", "56789", "0", BankAccountType.Current),
        Address: new EstablishmentAddress("Rua Exemplo", "100", "Centro", "São Paulo", "SP", "01310100"));

    private const string EstablishmentJson = """
        {
            "estabelecimentoId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
            "documento": "12345678000199",
            "razaoSocial": "Loja Centro LTDA",
            "nomeFantasia": "Loja Centro",
            "ativa": true,
            "modeloDesembolso": 1,
            "responsavel": { "nome": "Maria Souza", "email": "maria@loja.com.br", "celular": "+5511999998888" },
            "contaBancaria": {
                "banco": "341", "agencia": "1234", "digitoAgencia": "", "conta": "56789",
                "digitoConta": "0", "tipoConta": 1, "nomeTitular": null, "documentoTitular": null
            },
            "endereco": {
                "rua": "Rua Exemplo", "numero": "100", "complemento": null, "bairro": "Centro",
                "cidade": "São Paulo", "estado": "SP", "cep": "01310100", "pais": "Brasil"
            }
        }
        """;

    [Fact]
    public async Task CreateAsync_EnviaOCorpoNoFormatoDaApiEDevolveOId()
    {
        string? capturedBody = null;
        Uri? capturedUri = null;

        var inner = new FakeHttpMessageHandler(async (request, _, _) =>
        {
            capturedUri = request.RequestUri;
            capturedBody = await request.Content!.ReadAsStringAsync();

            return JsonResponse("""{ "estabelecimentoId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d" }""");
        });

        var client = CreateClient(inner);

        var result = await client.CreateAsync(NewCreateRequest());

        Assert.Equal(Guid.Parse("9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"), result.EstablishmentId);
        Assert.EndsWith("v1/establishment", capturedUri!.AbsolutePath);
        Assert.Contains("\"documento\":\"12345678000199\"", capturedBody);
        Assert.Contains("\"razaoSocial\":\"Loja Centro LTDA\"", capturedBody);
        Assert.Contains("\"modeloDesembolso\":1", capturedBody);
        Assert.Contains("5511999998888", capturedBody);
        Assert.Contains("\"contaBancaria\"", capturedBody);
        Assert.Contains("\"endereco\"", capturedBody);
        Assert.DoesNotContain("\"endereco\":null", capturedBody);
        Assert.Contains("\"rua\":\"Rua Exemplo\"", capturedBody);
        Assert.Contains("\"numero\":\"100\"", capturedBody);
        Assert.Contains("\"bairro\":\"Centro\"", capturedBody);
        Assert.Contains("\"estado\":\"SP\"", capturedBody);
        Assert.Contains("\"cep\":\"01310100\"", capturedBody);
    }

    [Fact]
    public async Task GetAsync_DesserializaOExemploRealDaApi()
    {
        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(EstablishmentJson)));
        var client = CreateClient(inner);

        var establishment = await client.GetAsync(Guid.Parse("9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d"));

        Assert.Equal("Loja Centro", establishment.TradeName);
        Assert.Equal("Loja Centro LTDA", establishment.LegalName);
        Assert.Equal("12345678000199", establishment.Document);
        Assert.True(establishment.IsActive);
        Assert.Equal(DisbursementModel.EstablishmentChain, establishment.DisbursementModel);
        Assert.Equal("+5511999998888", establishment.Owner.Phone);
        Assert.Equal("341", establishment.BankAccount!.BankNumber);
        Assert.Equal(BankAccountType.Current, establishment.BankAccount.AccountType);
        Assert.Equal("Rua Exemplo", establishment.Address!.Street);
    }

    [Fact]
    public async Task GetAsync_SemContaBancariaNemEndereco_NaoLanca()
    {
        const string json = """
            {
                "estabelecimentoId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
                "documento": "12345678000199",
                "razaoSocial": "Loja Centro LTDA",
                "nomeFantasia": "Loja Centro",
                "ativa": false,
                "modeloDesembolso": null,
                "responsavel": { "nome": "Maria Souza", "email": "maria@loja.com.br", "celular": "+5511999998888" },
                "contaBancaria": null,
                "endereco": null
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var establishment = await client.GetAsync(Guid.NewGuid());

        Assert.False(establishment.IsActive);
        Assert.Null(establishment.DisbursementModel);
        Assert.Null(establishment.BankAccount);
        Assert.Null(establishment.Address);
    }

    [Fact]
    public async Task ListAsync_MontaAQueryStringComOsFiltrosInformados()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("[]"));
        });

        var client = CreateClient(inner);
        await client.ListAsync(new ListEstablishmentsRequest(TradeName: "Centro", IsActive: true));

        Assert.NotNull(capturedUri);
        Assert.EndsWith("v1/establishment/list", capturedUri!.AbsolutePath);
        Assert.Contains("nomeFantasia=Centro", capturedUri.Query);
        Assert.Contains("ativa=true", capturedUri.Query);
    }

    [Fact]
    public async Task ListAsync_SemFiltros_NaoEnviaQueryString()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("[]"));
        });

        var client = CreateClient(inner);
        await client.ListAsync();

        Assert.Equal(string.Empty, capturedUri!.Query);
    }

    [Fact]
    public async Task ListAsync_DesserializaAListaDeLojas()
    {
        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse($"[{EstablishmentJson}]")));
        var client = CreateClient(inner);

        var establishments = await client.ListAsync(new ListEstablishmentsRequest(IsActive: false));

        Assert.Single(establishments);
        Assert.Equal("Loja Centro", establishments[0].TradeName);
    }

    [Fact]
    public async Task UpdateAsync_EnviaApenasOsCamposDaEdicao()
    {
        string? capturedBody = null;
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;

        var inner = new FakeHttpMessageHandler(async (request, _, _) =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            capturedBody = await request.Content!.ReadAsStringAsync();

            return EmptyResponse();
        });

        var client = CreateClient(inner);
        var establishmentId = Guid.Parse("9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d");

        await client.UpdateAsync(establishmentId, new UpdateEstablishmentRequest("Loja Centro Matriz"));

        Assert.Equal(HttpMethod.Put, capturedMethod);
        Assert.EndsWith($"v1/establishment/{establishmentId}", capturedUri!.AbsolutePath);
        Assert.Contains("\"nomeFantasia\":\"Loja Centro Matriz\"", capturedBody);
        Assert.DoesNotContain("\"contaBancaria\"", capturedBody);
    }

    [Fact]
    public async Task UpdateBankAccountAsync_UsaOEndpointProprioDaContaBancaria()
    {
        string? capturedBody = null;
        Uri? capturedUri = null;

        var inner = new FakeHttpMessageHandler(async (request, _, _) =>
        {
            capturedUri = request.RequestUri;
            capturedBody = await request.Content!.ReadAsStringAsync();

            return EmptyResponse();
        });

        var client = CreateClient(inner);
        var establishmentId = Guid.Parse("9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d");

        await client.UpdateBankAccountAsync(
            establishmentId,
            new EstablishmentBankAccount("237", "4321", "98765", "1", BankAccountType.Savings));

        Assert.EndsWith($"v1/establishment/{establishmentId}/bank-account", capturedUri!.AbsolutePath);
        Assert.Contains("\"banco\":\"237\"", capturedBody);
        Assert.Contains("\"tipoConta\":2", capturedBody);
    }

    [Fact]
    public async Task ActivateAsync_EnviaAtivaVerdadeiro()
    {
        string? capturedBody = null;
        Uri? capturedUri = null;

        var inner = new FakeHttpMessageHandler(async (request, _, _) =>
        {
            capturedUri = request.RequestUri;
            capturedBody = await request.Content!.ReadAsStringAsync();

            return EmptyResponse();
        });

        var client = CreateClient(inner);
        var establishmentId = Guid.Parse("9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d");

        await client.ActivateAsync(establishmentId);

        Assert.EndsWith($"v1/establishment/{establishmentId}/status", capturedUri!.AbsolutePath);
        Assert.Contains("\"ativa\":true", capturedBody);
    }

    [Fact]
    public async Task DeactivateAsync_EnviaAtivaFalso()
    {
        string? capturedBody = null;

        var inner = new FakeHttpMessageHandler(async (request, _, _) =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();

            return EmptyResponse();
        });

        var client = CreateClient(inner);

        await client.DeactivateAsync(Guid.NewGuid());

        Assert.Contains("\"ativa\":false", capturedBody);
    }

    [Fact]
    public async Task CreateAsync_Com409_LancaParceleMaisApiException()
    {
        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(
            """{ "tipo": "Establishment.DocumentAlreadyAdded", "status": 409, "detalhe": "Documento já cadastrado." }""",
            HttpStatusCode.Conflict)));

        var client = CreateClient(inner);

        var exception = await Assert.ThrowsAsync<ParceleMaisApiException>(() => client.CreateAsync(NewCreateRequest()));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task GetAsync_Com404_LancaParceleMaisApiException()
    {
        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(
            """{ "tipo": "Establishment.EstablishmentNotFound", "status": 404, "detalhe": "Estabelecimento não encontrado." }""",
            HttpStatusCode.NotFound)));

        var client = CreateClient(inner);

        var exception = await Assert.ThrowsAsync<ParceleMaisApiException>(() => client.GetAsync(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
}
