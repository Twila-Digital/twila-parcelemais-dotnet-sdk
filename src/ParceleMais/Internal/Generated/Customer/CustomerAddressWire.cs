using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Customer;

internal sealed record CustomerAddressWire(
    [property: JsonPropertyName("rua")] string? Rua = null,
    [property: JsonPropertyName("cidade")] string? Cidade = null,
    [property: JsonPropertyName("estado")] string? Estado = null,
    [property: JsonPropertyName("bairro")] string? Bairro = null,
    [property: JsonPropertyName("cep")] string? Cep = null,
    [property: JsonPropertyName("pais")] string? Pais = null,
    [property: JsonPropertyName("numero")] string? Numero = null,
    [property: JsonPropertyName("complemento")] string? Complemento = null);
