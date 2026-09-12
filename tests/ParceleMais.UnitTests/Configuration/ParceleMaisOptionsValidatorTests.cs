using ParceleMais.Configuration;

namespace ParceleMais.UnitTests.Configuration;

public class ParceleMaisOptionsValidatorTests
{
    private readonly ParceleMaisOptionsValidator _validator = new();

    private static ParceleMaisOptions ValidOptions() => new()
    {
        ClientId = "client-id",
        ClientSecret = "client-secret"
    };

    [Fact]
    public void Validate_ComOpcoesValidas_RetornaSucesso()
    {
        var result = _validator.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_SemClientId_Falha(string? clientId)
    {
        var options = ValidOptions();
        options.ClientId = clientId!;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ClientId", result.FailureMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_SemClientSecret_Falha(string? clientSecret)
    {
        var options = ValidOptions();
        options.ClientSecret = clientSecret!;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ClientSecret", result.FailureMessage);
    }

    [Fact]
    public void Validate_ComClientSecretInvalido_NuncaExpoeOValorNaMensagem()
    {
        var options = ValidOptions();
        options.ClientSecret = "";

        var result = _validator.Validate(null, options);

        Assert.DoesNotContain(options.ClientId, result.FailureMessage);
    }

    [Fact]
    public void Validate_ComBaseUrlRelativa_Falha()
    {
        var options = ValidOptions();
        options.BaseUrl = new Uri("relative/path", UriKind.Relative);

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ComAttemptTimeoutMaiorQueTotalTimeout_Falha()
    {
        var options = ValidOptions();
        options.Resilience.TotalTimeout = TimeSpan.FromSeconds(5);
        options.Resilience.AttemptTimeout = TimeSpan.FromSeconds(10);

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ComMaxRetryAttemptsMenorQueUm_Falha()
    {
        var options = ValidOptions();
        options.Resilience.MaxRetryAttempts = 0;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Validate_ComCircuitBreakerFailureRatioForaDoIntervalo_Falha(double ratio)
    {
        var options = ValidOptions();
        options.Resilience.CircuitBreakerFailureRatio = ratio;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }
}
