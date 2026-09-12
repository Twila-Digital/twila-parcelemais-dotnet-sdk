using ParceleMais.Authentication;
using ParceleMais.Configuration;
using ParceleMais.Idempotency;
using ParceleMais.Resilience;

namespace ParceleMais.UnitTests.TestUtilities;

internal static class TestApiClientFactory
{
    private sealed class FakeTokenProvider : IAccessTokenProvider
    {
        public Task<string> GetTokenAsync(CancellationToken cancellationToken) => Task.FromResult("fake-token");
        public void Invalidate()
        {
        }
    }

    public static HttpClient CreateApiHttpClient(FakeHttpMessageHandler inner, ParceleMaisOptions? options = null)
    {
        options ??= new ParceleMaisOptions { ClientId = "id", ClientSecret = "secret" };

        var authHandler = new AuthenticationDelegatingHandler(new FakeTokenProvider()) { InnerHandler = inner };
        var resilienceHandler = new ResilienceDelegatingHandler(ResiliencePipelineFactory.Create(options.Resilience)) { InnerHandler = authHandler };
        var idempotencyHandler = new IdempotencyKeyDelegatingHandler(options) { InnerHandler = resilienceHandler };

        return new HttpClient(idempotencyHandler) { BaseAddress = options.ResolveBaseUrl() };
    }
}
