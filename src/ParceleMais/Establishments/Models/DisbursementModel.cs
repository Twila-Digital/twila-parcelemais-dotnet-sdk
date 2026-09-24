using ParceleMais.Serialization;

namespace ParceleMais.Establishments.Models;

public enum DisbursementModel
{
    EstablishmentChain = 1,
    Establishment = 2,
    External = 3,

    [UnknownValue]
    Unknown = -1
}
