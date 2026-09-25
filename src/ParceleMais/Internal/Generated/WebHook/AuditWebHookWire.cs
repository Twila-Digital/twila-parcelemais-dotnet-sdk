using System.Text.Json.Serialization;

namespace ParceleMais.Internal.Generated.WebHook;

internal sealed record AuditWebHookWire(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("tipo")] int Tipo,
    [property: JsonPropertyName("requisicao")] string Requisicao,
    [property: JsonPropertyName("resposta")] string Resposta,
    [property: JsonPropertyName("statusCode")] int StatusCode,
    [property: JsonPropertyName("dataCriacao")] DateTimeOffset DataCriacao);
