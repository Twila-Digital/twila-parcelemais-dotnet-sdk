using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.WebHook;

internal sealed record CreateWebHookResponseWire([property: JsonPropertyName("chaveAssinatura")] string ChaveAssinatura);
