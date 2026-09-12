using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.Order;

internal sealed record StartCdcSaleRequestWire([property: JsonPropertyName("pedidoId")] Guid PedidoId);
