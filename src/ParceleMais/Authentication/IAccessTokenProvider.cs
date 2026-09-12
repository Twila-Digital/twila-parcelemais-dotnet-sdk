namespace ParceleMais.Authentication;

internal interface IAccessTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);

    void Invalidate();
}
