using System.ComponentModel.DataAnnotations;

namespace ParceleMais.Configuration;

/// <summary>
/// Opções de configuração do client do Parcele+.
/// </summary>
public sealed class ParceleMaisOptions
{
    /// <summary>
    /// Identificador do cliente (<c>client_id</c>), fornecido pelo Parcele+.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "ClientId é obrigatório.")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Segredo do cliente (<c>client_secret</c>), fornecido pelo Parcele+.
    /// Nunca é incluído em logs, mensagens de exceção ou <see cref="object.ToString"/>.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "ClientSecret é obrigatório.")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Ambiente de destino das chamadas. Default: <see cref="ParceleMaisEnvironment.Production"/>.
    /// </summary>
    public ParceleMaisEnvironment Environment { get; set; } = ParceleMaisEnvironment.Production;

    /// <summary>
    /// URL base customizada. Quando definida, substitui a URL derivada de <see cref="Environment"/>.
    /// </summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// Opções da pipeline de resiliência (timeout, retry, circuit breaker, idempotência).
    /// </summary>
    public ParceleMaisResilienceOptions Resilience { get; set; } = new();

    internal Uri ResolveBaseUrl()
    {
        var uri = BaseUrl ?? Environment.ToBaseUri();

        // Sem "/" final, HttpClient.BaseAddress + caminho relativo substitui o último
        // segmento do path (RFC 3986 §5.3) em vez de concatenar.
        return uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(uri.AbsoluteUri + "/", UriKind.Absolute);
    }
}
