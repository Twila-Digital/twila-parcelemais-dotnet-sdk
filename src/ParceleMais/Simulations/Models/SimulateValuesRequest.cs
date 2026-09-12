namespace ParceleMais.Simulations.Models;

public sealed record SimulateValuesRequest(decimal Amount, int Term, CalculationValueType CalculationValueType = CalculationValueType.GrossAmount);
