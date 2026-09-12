using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Order;

internal sealed record OrderStatusWire(
    [property: JsonPropertyName("valor")] int Value,
    [property: JsonPropertyName("descricao")] string Description);
