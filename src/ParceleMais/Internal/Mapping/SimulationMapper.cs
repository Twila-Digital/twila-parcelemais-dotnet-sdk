using ParceleMais.Internal.Generated.Simulations;
using ParceleMais.Simulations.Models;

namespace ParceleMais.Internal.Mapping;

internal static class SimulationMapper
{
    public static InstallmentSimulation ToPublic(SimulateInstallmentWire wire) =>
        new(wire.ValorTotalDebito, wire.Prazo, wire.ValorParcela);

    public static ValuesSimulation ToPublic(SimulationValuesWire wire) => new(
        wire.ValoresEstabelecimento.ValorVenda,
        wire.ValoresEstabelecimento.ValorDesembolso,
        wire.ValoresCliente.ValorParcela);
}
