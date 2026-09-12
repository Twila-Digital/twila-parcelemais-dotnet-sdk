namespace ParceleMais.Simulations.Models;

public sealed record SimulateInstallmentsRequest(decimal RequestedAmount, CalculationValueType CalculationValueType = CalculationValueType.GrossAmount);
