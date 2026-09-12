using ParceleMais.Configuration;

namespace ParceleMais.UnitTests.Configuration;

public class ParceleMaisOptionsTests
{
    [Fact]
    public void ResolveBaseUrl_SemBaseUrlCustomizada_UsaAUrlDoAmbienteStaging()
    {
        var options = new ParceleMaisOptions { Environment = ParceleMaisEnvironment.Staging };

        var resolved = options.ResolveBaseUrl();

        Assert.Equal("https://api.staging.parcelemais.com.br/integration", resolved.ToString().TrimEnd('/'));
    }

    [Fact]
    public void ResolveBaseUrl_SemBaseUrlCustomizada_UsaAUrlDoAmbienteProduction()
    {
        var options = new ParceleMaisOptions { Environment = ParceleMaisEnvironment.Production };

        var resolved = options.ResolveBaseUrl();

        Assert.Equal("https://api.parcelemais.com.br/integration", resolved.ToString().TrimEnd('/'));
    }

    [Fact]
    public void ResolveBaseUrl_ComBaseUrlCustomizada_TemPrioridadeSobreOAmbiente()
    {
        var customUrl = new Uri("https://mock.local/integration");
        var options = new ParceleMaisOptions { Environment = ParceleMaisEnvironment.Production, BaseUrl = customUrl };

        var resolved = options.ResolveBaseUrl();

        Assert.Equal(customUrl, resolved);
    }
}
