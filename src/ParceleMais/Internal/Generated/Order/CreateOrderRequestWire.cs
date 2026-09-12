using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Order;

internal sealed record CreateOrderRequestWire(
    [property: JsonPropertyName("cpf")] string Cpf,
    [property: JsonPropertyName("celular")] string Celular,
    [property: JsonPropertyName("documentoEstabelecimento")] string DocumentoEstabelecimento,
    [property: JsonPropertyName("valorSolicitado")] decimal ValorSolicitado,
    [property: JsonPropertyName("nome")] string Nome,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("dataDeNascimento")] DateTimeOffset DataDeNascimento,
    [property: JsonPropertyName("endereco")] AddressWire Endereco);
