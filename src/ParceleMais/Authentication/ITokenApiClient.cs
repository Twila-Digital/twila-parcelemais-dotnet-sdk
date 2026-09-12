using ParceleMais.Authentication.Models;

namespace ParceleMais.Authentication;

internal interface ITokenApiClient
{
    Task<GenerateAccessTokenResponse> GenerateAsync(string clientId, string clientSecret, CancellationToken cancellationToken);
}
