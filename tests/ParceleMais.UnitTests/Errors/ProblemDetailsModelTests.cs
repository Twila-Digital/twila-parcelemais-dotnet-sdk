using System.Text.Json;
using ParceleMais.Errors;
using ParceleMais.Serialization;

namespace ParceleMais.UnitTests.Errors;

public class ProblemDetailsModelTests
{
    private static ProblemDetailsModel Deserialize(string json) =>
        JsonSerializer.Deserialize<ProblemDetailsModel>(json, ParceleMaisJsonOptions.Default)!;

    [Fact]
    public void Deserialize_ErroDeRegraDeNegocio_PopulaErrosECodigoDeDominio()
    {
        const string json = """
            {
                "tipo": "Order.TermOutOfRange",
                "titulo": "Requisição inválida.",
                "status": 400,
                "detalhe": "O prazo informado está fora do intervalo permitido.",
                "instancia": "/v1/order/simulate-values",
                "erros": { "prazo": ["O prazo informado está fora do intervalo permitido."] },
                "correlationId": "0HN7E4B8Q9K3D:00000001"
            }
            """;

        var result = Deserialize(json);

        Assert.Equal("Order.TermOutOfRange", result.Type);
        Assert.Equal("0HN7E4B8Q9K3D:00000001", result.CorrelationId);
        Assert.NotNull(result.Errors);
        Assert.Equal(["O prazo informado está fora do intervalo permitido."], result.Errors!["prazo"]);
    }

    [Fact]
    public void Deserialize_ErroDeValidacaoDePayloadMalformado_SemErrosNemCorrelationId_NaoLanca()
    {
        const string json = """
            {
                "tipo": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "titulo": "Requisição inválida.",
                "status": 400,
                "detalhe": null,
                "instancia": "/v1/order"
            }
            """;

        var result = Deserialize(json);

        Assert.Equal("https://tools.ietf.org/html/rfc7231#section-6.5.1", result.Type);
        Assert.Null(result.Detail);
        Assert.Null(result.Errors);
        Assert.Null(result.CorrelationId);
    }

    [Fact]
    public void Deserialize_ComCampoNovoNaoModelado_PreservaEmExtensionData_SemLancar()
    {
        const string json = """
            {
                "tipo": "Order.SomeNewError",
                "status": 422,
                "umCampoQueOSdkAindaNaoConhece": { "nested": true }
            }
            """;

        var result = Deserialize(json);

        Assert.Equal("Order.SomeNewError", result.Type);
        Assert.NotNull(result.ExtensionData);
        Assert.True(result.ExtensionData!.ContainsKey("umCampoQueOSdkAindaNaoConhece"));
    }

    [Fact]
    public void Deserialize_JsonVazio_NaoLancaEDevolveTudoNulo()
    {
        var result = Deserialize("{}");

        Assert.Null(result.Type);
        Assert.Null(result.Status);
        Assert.Null(result.Errors);
    }
}
