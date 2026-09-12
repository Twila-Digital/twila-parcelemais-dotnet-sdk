using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.WebHook;

internal sealed record OrderWebhookEventWire(
    [property: JsonPropertyName("id_pedido")] Guid IdPedido,
    [property: JsonPropertyName("enum_status")] int EnumStatus,
    [property: JsonPropertyName("status")] string Status);
