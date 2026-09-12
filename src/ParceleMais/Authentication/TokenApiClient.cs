using System.Net.Http.Json;
using ParceleMais.Authentication.Models;
using ParceleMais.Errors;
using ParceleMais.Serialization;

namespace ParceleMais.Authentication;

internal sealed class TokenApiClient(HttpClient httpClient) : ITokenApiClient
{
    public async Task<GenerateAccessTokenResponse> GenerateAsync(string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        var request = new GenerateAccessTokenRequest(clientId, clientSecret);

        using var response = await httpClient
            .PostAsJsonAsync("v1/authentication/accesstoken", request, ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw await ParceleMaisExceptionFactory.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);

        var result = await response.Content
            .ReadFromJsonAsync<GenerateAccessTokenResponse>(ParceleMaisJsonOptions.Default, cancellationToken)
            .ConfigureAwait(false);

        return result ?? throw new ParceleMaisAuthenticationException("A API do Parcele+ retornou uma resposta vazia ao gerar o token de acesso.");
    }
}
