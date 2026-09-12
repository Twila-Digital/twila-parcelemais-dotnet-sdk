using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Customer;

internal sealed record CustomerWire(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("documento")] string Documento,
    [property: JsonPropertyName("dataDeNascimento")] DateTimeOffset DataDeNascimento,
    [property: JsonPropertyName("endereco")] CustomerAddressWire? Endereco = null,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("celular")] string? Celular = null);
