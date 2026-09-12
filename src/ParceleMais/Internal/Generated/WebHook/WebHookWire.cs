using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.WebHook;

internal sealed record WebHookWire(
    [property: JsonPropertyName("tipo")] int Tipo,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("tipoAutenticacao")] int TipoAutenticacao);
