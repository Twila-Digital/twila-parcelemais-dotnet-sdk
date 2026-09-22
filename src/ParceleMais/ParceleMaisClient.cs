using ParceleMais.Customers;
using ParceleMais.Orders;
using ParceleMais.Simulations;
using ParceleMais.Establishments;
using ParceleMais.Webhooks;

namespace ParceleMais;

internal sealed class ParceleMaisClient(
    IOrdersClient orders,
    ISimulationsClient simulations,
    ICustomersClient customers,
    IEstablishmentsClient establishments,
    IWebhooksClient webhooks) : IParceleMaisClient
{
    public IOrdersClient Orders { get; } = orders;

    public ISimulationsClient Simulations { get; } = simulations;

    public ICustomersClient Customers { get; } = customers;

    public IEstablishmentsClient Establishments { get; } = establishments;

    public IWebhooksClient Webhooks { get; } = webhooks;
}
