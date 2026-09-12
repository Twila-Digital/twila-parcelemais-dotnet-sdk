using ParceleMais.Orders.Models;

namespace ParceleMais.Webhooks.Models;

public sealed record OrderWebhookEvent(Guid OrderId, OrderStatus Status, int StatusRaw, string StatusName);
