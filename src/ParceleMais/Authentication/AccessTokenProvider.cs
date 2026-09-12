using ParceleMais.Configuration;

namespace ParceleMais.Authentication;

internal sealed class AccessTokenProvider(ITokenApiClient tokenApiClient, ParceleMaisOptions options) : IAccessTokenProvider, IDisposable
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private AccessToken? _cached;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var current = _cached;
        if (current is not null && !current.IsCloseToExpiry(ClockSkew))
            return current.Value;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            current = _cached;
            if (current is not null && !current.IsCloseToExpiry(ClockSkew))
                return current.Value;

            var response = await tokenApiClient.GenerateAsync(options.ClientId, options.ClientSecret, cancellationToken).ConfigureAwait(false);
            var fresh = new AccessToken(response.AccessToken, DateTimeOffset.UtcNow.AddSeconds(response.ExpiresInSeconds));

            _cached = fresh;
            return fresh.Value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _cached = null;

    public void Dispose() => _lock.Dispose();
}
