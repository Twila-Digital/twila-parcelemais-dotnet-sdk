using ParceleMais.Serialization;

namespace ParceleMais.Simulations.Models;

public enum CalculationValueType
{
    GrossAmount = 1,
    LiquidAmount = 2,

    [UnknownValue]
    Unknown = -1
}
