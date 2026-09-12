using ParceleMais.Customers;
using ParceleMais.Orders;
using ParceleMais.Simulations;
using ParceleMais.Webhooks;

namespace ParceleMais;

public interface IParceleMaisClient
{
    IOrdersClient Orders { get; }

    ISimulationsClient Simulations { get; }

    ICustomersClient Customers { get; }

    IWebhooksClient Webhooks { get; }
}
