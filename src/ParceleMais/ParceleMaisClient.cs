using ParceleMais.Orders;
using ParceleMais.Simulations;

namespace ParceleMais;

internal sealed class ParceleMaisClient(IOrdersClient orders, ISimulationsClient simulations) : IParceleMaisClient
{
    public IOrdersClient Orders { get; } = orders;

    public ISimulationsClient Simulations { get; } = simulations;
}
