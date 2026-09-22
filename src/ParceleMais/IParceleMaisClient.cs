using ParceleMais.Customers;
using ParceleMais.Orders;
using ParceleMais.Simulations;
using ParceleMais.Establishments;
using ParceleMais.Webhooks;

namespace ParceleMais;

public interface IParceleMaisClient
{
    IOrdersClient Orders { get; }

    ISimulationsClient Simulations { get; }

    ICustomersClient Customers { get; }

    IEstablishmentsClient Establishments { get; }

    IWebhooksClient Webhooks { get; }
}
