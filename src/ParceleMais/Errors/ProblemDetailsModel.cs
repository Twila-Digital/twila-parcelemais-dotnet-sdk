using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParceleMais.Errors;

/// <summary>
/// Modelo do <c>ProblemDetails</c> (campos em português) devolvido pela API em respostas de erro.
/// Campos ausentes/nulos/desconhecidos nunca causam falha de desserialização.
/// </summary>
public sealed class ProblemDetailsModel
{
    [JsonPropertyName("tipo")]
    public string? Type { get; set; }

    [JsonPropertyName("titulo")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detalhe")]
    public string? Detail { get; set; }

    [JsonPropertyName("instancia")]
    public string? Instance { get; set; }

    [JsonPropertyName("erros")]
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Campos não mapeados nas propriedades acima.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    internal static ProblemDetailsModel Empty(string? fallbackDetail = null) => new()
    {
        Detail = fallbackDetail
    };
}
