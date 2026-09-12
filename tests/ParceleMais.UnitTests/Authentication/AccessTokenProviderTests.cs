using ParceleMais.Authentication;
using ParceleMais.Authentication.Models;
using ParceleMais.Configuration;

namespace ParceleMais.UnitTests.Authentication;

public class AccessTokenProviderTests
{
    private sealed class CountingTokenApiClient(Func<int, GenerateAccessTokenResponse> respond) : ITokenApiClient
    {
        private int _callCount;

        public int CallCount => _callCount;

        public Task<GenerateAccessTokenResponse> GenerateAsync(string clientId, string clientSecret, CancellationToken cancellationToken)
        {
            var callIndex = Interlocked.Increment(ref _callCount);
            return Task.FromResult(respond(callIndex));
        }
    }

    private static ParceleMaisOptions Options() => new() { ClientId = "id", ClientSecret = "secret" };

    [Fact]
    public async Task GetTokenAsync_PrimeiraChamada_GeraUmTokenNovo()
    {
        var tokenApiClient = new CountingTokenApiClient(_ => new GenerateAccessTokenResponse("token-1", 3600, "Bearer"));
        var provider = new AccessTokenProvider(tokenApiClient, Options());

        var token = await provider.GetTokenAsync(CancellationToken.None);

        Assert.Equal("token-1", token);
        Assert.Equal(1, tokenApiClient.CallCount);
    }

    [Fact]
    public async Task GetTokenAsync_ComTokenAindaValido_NaoGeraNovoToken()
    {
        var tokenApiClient = new CountingTokenApiClient(callIndex => new GenerateAccessTokenResponse($"token-{callIndex}", 3600, "Bearer"));
        var provider = new AccessTokenProvider(tokenApiClient, Options());

        var first = await provider.GetTokenAsync(CancellationToken.None);
        var second = await provider.GetTokenAsync(CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(1, tokenApiClient.CallCount);
    }

    [Fact]
    public async Task GetTokenAsync_ComTokenPertoDeExpirar_GeraUmNovoToken()
    {
        var tokenApiClient = new CountingTokenApiClient(callIndex => new GenerateAccessTokenResponse($"token-{callIndex}", 30, "Bearer"));
        var provider = new AccessTokenProvider(tokenApiClient, Options());

        var first = await provider.GetTokenAsync(CancellationToken.None);
        var second = await provider.GetTokenAsync(CancellationToken.None);

        Assert.NotEqual(first, second);
        Assert.Equal(2, tokenApiClient.CallCount);
    }

    [Fact]
    public async Task Invalidate_ForcaGeracaoDeUmNovoTokenNaProximaChamada()
    {
        var tokenApiClient = new CountingTokenApiClient(callIndex => new GenerateAccessTokenResponse($"token-{callIndex}", 3600, "Bearer"));
        var provider = new AccessTokenProvider(tokenApiClient, Options());

        var first = await provider.GetTokenAsync(CancellationToken.None);
        provider.Invalidate();
        var second = await provider.GetTokenAsync(CancellationToken.None);

        Assert.NotEqual(first, second);
        Assert.Equal(2, tokenApiClient.CallCount);
    }

    [Fact]
    public async Task GetTokenAsync_Com100ChamadasConcorrentes_GeraApenasUmToken()
    {
        var tokenApiClient = new CountingTokenApiClient(callIndex => new GenerateAccessTokenResponse($"token-{callIndex}", 3600, "Bearer"));
        var provider = new AccessTokenProvider(tokenApiClient, Options());

        var tasks = Enumerable.Range(0, 100).Select(_ => provider.GetTokenAsync(CancellationToken.None));
        var tokens = await Task.WhenAll(tasks);

        Assert.Equal(1, tokenApiClient.CallCount);
        Assert.All(tokens, t => Assert.Equal("token-1", t));
    }
}
