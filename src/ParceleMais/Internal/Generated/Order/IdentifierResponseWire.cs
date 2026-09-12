using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Order;

internal sealed record IdentifierResponseWire([property: JsonPropertyName("pedidoId")] Guid PedidoId);
