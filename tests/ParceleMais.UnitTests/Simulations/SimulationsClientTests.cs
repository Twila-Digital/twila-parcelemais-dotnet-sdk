using System.Net;
using System.Net.Http.Json;
using ParceleMais.Simulations;
using ParceleMais.Simulations.Models;
using ParceleMais.UnitTests.TestUtilities;

namespace ParceleMais.UnitTests.Simulations;

public class SimulationsClientTests
{
    private static ISimulationsClient CreateClient(FakeHttpMessageHandler inner) =>
        new SimulationsClient(TestApiClientFactory.CreateApiHttpClient(inner));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(System.Text.Json.JsonDocument.Parse(json).RootElement) };

    [Fact]
    public async Task SimulateInstallmentsAsync_DesserializaOExemploRealDaApi()
    {
        const string json = """[{ "valorTotalDebito": 1632.48, "prazo": 12, "valorParcela": 136.04 }]""";

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var result = await client.SimulateInstallmentsAsync(new SimulateInstallmentsRequest(1500.00m));

        Assert.Single(result);
        Assert.Equal(1632.48m, result[0].TotalAmount);
        Assert.Equal(12, result[0].Term);
        Assert.Equal(136.04m, result[0].InstallmentAmount);
    }

    [Fact]
    public async Task SimulateInstallmentsAsync_EnviaOTipoDeCalculoNaQueryString()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("[]"));
        });

        var client = CreateClient(inner);
        await client.SimulateInstallmentsAsync(new SimulateInstallmentsRequest(1500.00m, CalculationValueType.LiquidAmount));

        Assert.Contains("tipoValorCalculo=2", capturedUri!.Query);
        Assert.Contains("valorSolicitado=1500", capturedUri.Query);
    }

    [Fact]
    public async Task SimulateValuesAsync_DesserializaOExemploRealDaApi()
    {
        const string json = """
            {
                "valoresEstabelecimento": { "valorVenda": 7054.673721340388, "valorDesembolso": 5959.26 },
                "valoresCliente": { "valorParcela": 718.46 }
            }
            """;

        var inner = new FakeHttpMessageHandler((_, _, _) => Task.FromResult(JsonResponse(json)));
        var client = CreateClient(inner);

        var result = await client.SimulateValuesAsync(new SimulateValuesRequest(1500.00m, 12));

        Assert.Equal(5959.26m, result.DisbursementAmount);
        Assert.Equal(718.46m, result.InstallmentAmount);
    }

    [Fact]
    public async Task SimulateValuesAsync_SempreEnviaModeloJurosFixoEm1()
    {
        Uri? capturedUri = null;
        var inner = new FakeHttpMessageHandler((request, _, _) =>
        {
            capturedUri = request.RequestUri;
            return Task.FromResult(JsonResponse("""{ "valoresEstabelecimento": {"valorVenda":0,"valorDesembolso":0}, "valoresCliente": {"valorParcela":0} }"""));
        });

        var client = CreateClient(inner);
        await client.SimulateValuesAsync(new SimulateValuesRequest(1500.00m, 12));

        Assert.Contains("modeloJuros=1", capturedUri!.Query);
    }
}
