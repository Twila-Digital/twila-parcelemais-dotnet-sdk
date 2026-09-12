using Microsoft.Extensions.Options;

namespace ParceleMais.Configuration;

/// <summary>
/// Valida <see cref="ParceleMaisOptions"/> além do que <see cref="System.ComponentModel.DataAnnotations"/> cobre.
/// </summary>
internal sealed class ParceleMaisOptionsValidator : IValidateOptions<ParceleMaisOptions>
{
    public ValidateOptionsResult Validate(string? name, ParceleMaisOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
            return ValidateOptionsResult.Fail("ClientId é obrigatório.");

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            return ValidateOptionsResult.Fail("ClientSecret é obrigatório.");

        if (options.BaseUrl is not null && !options.BaseUrl.IsAbsoluteUri)
            return ValidateOptionsResult.Fail("BaseUrl, quando informada, deve ser uma URI absoluta.");

        var resilience = options.Resilience;

        if (resilience.MaxRetryAttempts < 1)
            return ValidateOptionsResult.Fail("Resilience.MaxRetryAttempts deve ser maior ou igual a 1.");

        if (resilience.TotalTimeout <= TimeSpan.Zero)
            return ValidateOptionsResult.Fail("Resilience.TotalTimeout deve ser maior que zero.");

        if (resilience.AttemptTimeout <= TimeSpan.Zero)
            return ValidateOptionsResult.Fail("Resilience.AttemptTimeout deve ser maior que zero.");

        if (resilience.AttemptTimeout > resilience.TotalTimeout)
            return ValidateOptionsResult.Fail("Resilience.AttemptTimeout não pode ser maior que Resilience.TotalTimeout.");

        if (resilience.CircuitBreakerFailureRatio is <= 0 or > 1)
            return ValidateOptionsResult.Fail("Resilience.CircuitBreakerFailureRatio deve estar entre 0 (exclusivo) e 1 (inclusivo).");

        if (resilience.CircuitBreakerMinimumThroughput < 2)
            return ValidateOptionsResult.Fail("Resilience.CircuitBreakerMinimumThroughput deve ser maior ou igual a 2.");

        return ValidateOptionsResult.Success;
    }
}
