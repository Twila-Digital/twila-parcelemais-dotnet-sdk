using ParceleMais.Serialization;

namespace ParceleMais.Establishments.Models;

public enum BankAccountType
{
    Current = 1,
    Savings = 2,
    Payment = 3,

    [UnknownValue]
    Unknown = -1
}
