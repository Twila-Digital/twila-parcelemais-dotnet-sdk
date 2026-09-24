using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Establishment;

internal sealed record EstablishmentAddressWire(
    [property: JsonPropertyName("rua")] string Rua,
    [property: JsonPropertyName("numero")] string Numero,
    [property: JsonPropertyName("bairro")] string Bairro,
    [property: JsonPropertyName("cidade")] string Cidade,
    [property: JsonPropertyName("estado")] string Estado,
    [property: JsonPropertyName("cep")] string Cep,
    [property: JsonPropertyName("complemento")] string? Complemento = null,
    [property: JsonPropertyName("pais")] string? Pais = null);
