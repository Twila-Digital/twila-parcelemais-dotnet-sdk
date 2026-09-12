using ParceleMais.Orders;
using ParceleMais.Simulations;

namespace ParceleMais;

public interface IParceleMaisClient
{
    IOrdersClient Orders { get; }

    ISimulationsClient Simulations { get; }
}
