using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Order;

internal sealed record OrderWire(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("numero")] long Numero,
    [property: JsonPropertyName("status")] OrderStatusWire Status,
    [property: JsonPropertyName("documentoCliente")] string DocumentoCliente,
    [property: JsonPropertyName("razaoSocialEstabelecimento")] string RazaoSocialEstabelecimento,
    [property: JsonPropertyName("documentoEstabelecimento")] string DocumentoEstabelecimento,
    [property: JsonPropertyName("criadoEm")] DateTimeOffset CriadoEm,
    [property: JsonPropertyName("total")] decimal? Total = null,
    [property: JsonPropertyName("nomeCliente")] string? NomeCliente = null,
    [property: JsonPropertyName("prazo")] int? Prazo = null,
    [property: JsonPropertyName("descricao")] string? Descricao = null,
    [property: JsonPropertyName("valorAprovado")] decimal? ValorAprovado = null,
    [property: JsonPropertyName("desembolsado")] bool? Desembolsado = null,
    [property: JsonPropertyName("desembolsadoEm")] DateTimeOffset? DesembolsadoEm = null,
    [property: JsonPropertyName("valorSolicitado")] decimal? ValorSolicitado = null);
