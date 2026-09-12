using System.Text.Json.Serialization;

namespace ParceleMais.Authentication.Models;

internal sealed record GenerateAccessTokenRequest(
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("clientSecret")] string ClientSecret);
