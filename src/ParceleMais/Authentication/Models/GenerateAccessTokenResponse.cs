using System.Text.Json.Serialization;

namespace ParceleMais.Authentication.Models;

internal sealed record GenerateAccessTokenResponse(
    [property: JsonPropertyName("token_de_acesso")] string AccessToken,
    [property: JsonPropertyName("expira_em_segundos")] int ExpiresInSeconds,
    [property: JsonPropertyName("tipo_de_token")] string TokenType);
