using System.Text.Json;
using ParceleMais.Serialization;

namespace ParceleMais.UnitTests.Serialization;

public class UnknownEnumJsonConverterFactoryTests
{
    public enum SampleStatus
    {
        Analysing = 1,
        Approved = 2,

        [UnknownValue]
        Unknown = -1
    }

    private enum EnumSemUnknownValue
    {
        A = 1,
        B = 2
    }

    private static JsonSerializerOptions Options() => ParceleMaisJsonOptions.Default;

    [Theory]
    [InlineData("1", SampleStatus.Analysing)]
    [InlineData("2", SampleStatus.Approved)]
    public void Read_ComValorConhecido_DesserializaParaOMembroCorreto(string json, SampleStatus expected)
    {
        var result = JsonSerializer.Deserialize<SampleStatus>(json, Options());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("19")]
    [InlineData("999")]
    [InlineData("-7")]
    public void Read_ComValorNaoDefinidoNoEnum_CaiParaOMembroUnknown_SemLancar(string json)
    {
        var result = JsonSerializer.Deserialize<SampleStatus>(json, Options());

        Assert.Equal(SampleStatus.Unknown, result);
    }

    [Fact]
    public void Read_ComTokenQueNaoENumero_CaiParaUnknown_SemLancar()
    {
        var result = JsonSerializer.Deserialize<SampleStatus>("\"Approved\"", Options());

        Assert.Equal(SampleStatus.Unknown, result);
    }

    [Fact]
    public void Write_SerializaComoOInteiroCru_NaoComoString()
    {
        var json = JsonSerializer.Serialize(SampleStatus.Approved, Options());

        Assert.Equal("2", json);
    }

    [Fact]
    public void CanConvert_ParaEnumSemMembroUnknownValue_RetornaFalse()
    {
        var factory = new UnknownEnumJsonConverterFactory();

        Assert.False(factory.CanConvert(typeof(EnumSemUnknownValue)));
    }

    [Fact]
    public void Deserialize_EnumSemMembroUnknownValue_UsaComportamentoPadraoDoSystemTextJson()
    {
        // Sem [UnknownValue], o converter customizado não entra em jogo — o comportamento
        // default do System.Text.Json para enum numérico é aceitar qualquer int, mesmo não definido.
        var result = JsonSerializer.Deserialize<EnumSemUnknownValue>("42", Options());

        Assert.Equal((EnumSemUnknownValue)42, result);
    }

    [Fact]
    public void RoundTrip_EmUmaListaComValorConhecidoEDesconhecidoMisturados_PreservaAmbos()
    {
        const string json = "[1,2,999]";

        var result = JsonSerializer.Deserialize<SampleStatus[]>(json, Options());

        Assert.Equal([SampleStatus.Analysing, SampleStatus.Approved, SampleStatus.Unknown], result);
    }
}
