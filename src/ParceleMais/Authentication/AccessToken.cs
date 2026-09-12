namespace ParceleMais.Authentication;

internal sealed record AccessToken(string Value, DateTimeOffset ExpiresAt)
{
    public bool IsCloseToExpiry(TimeSpan clockSkew, DateTimeOffset? now = null) =>
        (now ?? DateTimeOffset.UtcNow) + clockSkew >= ExpiresAt;
}
