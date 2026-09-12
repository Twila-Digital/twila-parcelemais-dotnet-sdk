using ParceleMais.Configuration;

namespace ParceleMais.UnitTests.Configuration;

public class ParceleMaisOptionsTests
{
    [Fact]
    public void ResolveBaseUrl_SemBaseUrlCustomizada_UsaAUrlDoAmbienteStaging()
    {
        var options = new ParceleMaisOptions { Environment = ParceleMaisEnvironment.Staging };

        var resolved = options.ResolveBaseUrl();

        Assert.Equal("https://api.staging.parcelemais.com.br/integration/", resolved.ToString());
    }

    [Fact]
    public void ResolveBaseUrl_SemBaseUrlCustomizada_UsaAUrlDoAmbienteProduction()
    {
        var options = new ParceleMaisOptions { Environment = ParceleMaisEnvironment.Production };

        var resolved = options.ResolveBaseUrl();

        Assert.Equal("https://api.parcelemais.com.br/integration/", resolved.ToString());
    }

    [Fact]
    public void ResolveBaseUrl_ComBaseUrlCustomizada_TemPrioridadeSobreOAmbiente()
    {
        var customUrl = new Uri("https://mock.local/integration/");
        var options = new ParceleMaisOptions { Environment = ParceleMaisEnvironment.Production, BaseUrl = customUrl };

        var resolved = options.ResolveBaseUrl();

        Assert.Equal(customUrl, resolved);
    }

    [Fact]
    public void ResolveBaseUrl_ComBaseUrlCustomizadaSemBarraFinal_AdicionaABarraFinal()
    {
        var options = new ParceleMaisOptions { BaseUrl = new Uri("https://mock.local/integration") };

        var resolved = options.ResolveBaseUrl();

        Assert.Equal("https://mock.local/integration/", resolved.ToString());
    }

    [Theory]
    [InlineData(ParceleMaisEnvironment.Staging)]
    [InlineData(ParceleMaisEnvironment.Production)]
    public void ResolveBaseUrl_CombinadaComCaminhoRelativo_PreservaOSegmentoIntegration(ParceleMaisEnvironment environment)
    {
        var options = new ParceleMaisOptions { Environment = environment };

        var resolved = options.ResolveBaseUrl();
        var combined = new Uri(resolved, "v1/authentication/accesstoken");

        Assert.Contains("/integration/v1/authentication/accesstoken", combined.ToString());
    }
}
