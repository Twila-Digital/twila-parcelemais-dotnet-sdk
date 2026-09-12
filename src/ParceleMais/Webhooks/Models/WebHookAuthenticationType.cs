using ParceleMais.Serialization;

namespace ParceleMais.Webhooks.Models;

public enum WebHookAuthenticationType
{
    None = 1,
    Basic = 2,
    Jwt = 3,

    [UnknownValue]
    Unknown = -1
}
