using ParceleMais.Simulations.Models;

namespace ParceleMais.Simulations;

public interface ISimulationsClient
{
    Task<IReadOnlyList<InstallmentSimulation>> SimulateInstallmentsAsync(SimulateInstallmentsRequest request, CancellationToken cancellationToken = default);

    Task<ValuesSimulation> SimulateValuesAsync(SimulateValuesRequest request, CancellationToken cancellationToken = default);
}
