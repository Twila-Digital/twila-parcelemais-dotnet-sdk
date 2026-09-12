using System.Net;
using ParceleMais.Authentication;
using ParceleMais.Errors;
using ParceleMais.UnitTests.TestUtilities;

namespace ParceleMais.UnitTests.Authentication;

public class AuthenticationDelegatingHandlerTests
{
    private sealed class FakeTokenProvider : IAccessTokenProvider
    {
        private int _tokenVersion;
        public int InvalidateCallCount { get; private set; }

        public Task<string> GetTokenAsync(CancellationToken cancellationToken) => Task.FromResult($"token-v{_tokenVersion}");

        public void Invalidate()
        {
            InvalidateCallCount++;
            _tokenVersion++;
        }
    }

    private static HttpClient BuildClient(FakeHttpMessageHandler inner, FakeTokenProvider tokenProvider)
    {
        var authHandler = new AuthenticationDelegatingHandler(tokenProvider) { InnerHandler = inner };
        return new HttpClient(authHandler) { BaseAddress = new Uri("https://api.parcelemais.com.br/integration/") };
    }

    [Fact]
    public async Task SendAsync_AnexaBearerTokenNaRequisicao()
    {
        var tokenProvider = new FakeTokenProvider();
        string? capturedAuthHeader = null;
        var inner = new FakeHttpMessageHandler((request, _) =>
        {
            capturedAuthHeader = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = BuildClient(inner, tokenProvider);
        await client.GetAsync("v1/order/123");

        Assert.Equal("Bearer token-v0", capturedAuthHeader);
    }

    [Fact]
    public async Task SendAsync_Com401NaPrimeiraTentativa_InvalidaERenovaOToken()
    {
        var tokenProvider = new FakeTokenProvider();
        var inner = new FakeHttpMessageHandler((_, callIndex) => Task.FromResult(
            new HttpResponseMessage(callIndex == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)));

        var client = BuildClient(inner, tokenProvider);
        var response = await client.GetAsync("v1/order/123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, tokenProvider.InvalidateCallCount);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_Com401Persistente_LancaParceleMaisAuthenticationException_SemLoopInfinito()
    {
        var tokenProvider = new FakeTokenProvider();
        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var client = BuildClient(inner, tokenProvider);

        await Assert.ThrowsAsync<ParceleMaisAuthenticationException>(() => client.GetAsync("v1/order/123"));
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_ComCorpoNaRequisicao_ReenviaOMesmoCorpoNaSegundaTentativa()
    {
        var tokenProvider = new FakeTokenProvider();
        var capturedBodies = new List<string>();
        var inner = new FakeHttpMessageHandler(async (request, callIndex) =>
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
            capturedBodies.Add(body);
            return new HttpResponseMessage(callIndex == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
        });

        var client = BuildClient(inner, tokenProvider);
        await client.PostAsync("v1/order", new StringContent("{\"valorSolicitado\":300}"));

        Assert.Equal(2, capturedBodies.Count);
        Assert.All(capturedBodies, b => Assert.Equal("{\"valorSolicitado\":300}", b));
    }
}
