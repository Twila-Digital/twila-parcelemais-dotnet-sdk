using ParceleMais.Serialization;

namespace ParceleMais.Webhooks.Models;

public enum WebHookType
{
    Customer = 1,
    Simulation = 2,
    Order = 3,

    [UnknownValue]
    Unknown = -1
}
