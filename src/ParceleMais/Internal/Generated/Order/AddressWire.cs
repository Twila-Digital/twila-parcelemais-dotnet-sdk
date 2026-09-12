using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Order;

internal sealed record AddressWire(
    [property: JsonPropertyName("logradouro")] string Logradouro,
    [property: JsonPropertyName("numero")] string Numero,
    [property: JsonPropertyName("bairro")] string Bairro,
    [property: JsonPropertyName("cidade")] string Cidade,
    [property: JsonPropertyName("estado")] string Estado,
    [property: JsonPropertyName("cep")] string Cep,
    [property: JsonPropertyName("complemento")] string? Complemento = null);
