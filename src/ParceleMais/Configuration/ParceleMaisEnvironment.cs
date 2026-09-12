namespace ParceleMais.Configuration;

/// <summary>
/// Ambiente da API do Parcele+ a ser utilizado pelo client.
/// </summary>
public enum ParceleMaisEnvironment
{
    /// <summary>
    /// Ambiente de staging (<c>https://api.staging.parcelemais.com.br/integration</c>).
    /// </summary>
    Staging,

    /// <summary>
    /// Ambiente de produção (<c>https://api.parcelemais.com.br/integration</c>).
    /// </summary>
    Production
}

internal static class ParceleMaisEnvironmentExtensions
{
    private const string StagingBaseUrl = "https://api.staging.parcelemais.com.br/integration";
    private const string ProductionBaseUrl = "https://api.parcelemais.com.br/integration";

    public static Uri ToBaseUri(this ParceleMaisEnvironment environment) => environment switch
    {
        ParceleMaisEnvironment.Staging => new Uri(StagingBaseUrl, UriKind.Absolute),
        ParceleMaisEnvironment.Production => new Uri(ProductionBaseUrl, UriKind.Absolute),
        _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Ambiente do Parcele+ desconhecido.")
    };
}
