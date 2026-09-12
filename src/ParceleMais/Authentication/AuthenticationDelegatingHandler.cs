using System.Net;
using System.Net.Http.Headers;
using ParceleMais.Errors;
using ParceleMais.Http;

namespace ParceleMais.Authentication;

internal sealed class AuthenticationDelegatingHandler(IAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var firstAttempt = await request.CloneAsync().ConfigureAwait(false);
        var token = await tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        firstAttempt.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(firstAttempt, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        tokenProvider.Invalidate();
        response.Dispose();

        var secondAttempt = await request.CloneAsync().ConfigureAwait(false);
        var newToken = await tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        secondAttempt.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        var secondResponse = await base.SendAsync(secondAttempt, cancellationToken).ConfigureAwait(false);

        if (secondResponse.StatusCode != HttpStatusCode.Unauthorized)
            return secondResponse;

        var exception = await ParceleMaisExceptionFactory.FromResponseAsync(secondResponse, cancellationToken).ConfigureAwait(false);
        secondResponse.Dispose();
        throw exception;
    }
}
