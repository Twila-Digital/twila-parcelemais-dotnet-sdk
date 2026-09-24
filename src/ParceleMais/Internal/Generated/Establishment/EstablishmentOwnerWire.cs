using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Establishment;

internal sealed record EstablishmentOwnerWire(
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("celular")] string Celular);
