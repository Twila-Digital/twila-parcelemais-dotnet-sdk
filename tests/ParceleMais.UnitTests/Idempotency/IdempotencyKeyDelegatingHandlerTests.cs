using System.Net;
using ParceleMais.Configuration;
using ParceleMais.Idempotency;
using ParceleMais.UnitTests.TestUtilities;

namespace ParceleMais.UnitTests.Idempotency;

public class IdempotencyKeyDelegatingHandlerTests
{
    private static HttpClient BuildClient(FakeHttpMessageHandler inner, ParceleMaisOptions? options = null)
    {
        var handler = new IdempotencyKeyDelegatingHandler(options ?? new ParceleMaisOptions { ClientId = "id", ClientSecret = "secret" })
        {
            InnerHandler = inner
        };
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.parcelemais.com.br/integration/") };
    }

    [Theory]
    [InlineData("v1/order")]
    [InlineData("v1/order/start-cdc-sale")]
    [InlineData("v1/order/invoice")]
    [InlineData("v1/webhooks")]
    public async Task SendAsync_EmEndpointMutavel_AnexaIdempotencyKey(string path)
    {
        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = BuildClient(inner);

        await client.PostAsync(path, new StringContent("{}"));

        Assert.True(inner.Requests[0].Headers.Contains("Idempotency-Key"));
    }

    [Theory]
    [InlineData("v1/order/simulate-values")]
    [InlineData("v1/order/paged")]
    public async Task SendAsync_EmGet_NaoAnexaIdempotencyKey(string path)
    {
        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = BuildClient(inner);

        await client.GetAsync(path);

        Assert.False(inner.Requests[0].Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task SendAsync_EmPutWebhook_NaoAnexaIdempotencyKey()
    {
        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = BuildClient(inner);

        await client.PutAsync("v1/webhooks/3", new StringContent("{}"));

        Assert.False(inner.Requests[0].Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task SendAsync_ChamadasDiferentesParaOMesmoEndpoint_GeramChavesDiferentes()
    {
        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = BuildClient(inner);

        await client.PostAsync("v1/order", new StringContent("{}"));
        await client.PostAsync("v1/order", new StringContent("{}"));

        var firstKey = inner.Requests[0].Headers.GetValues("Idempotency-Key").Single();
        var secondKey = inner.Requests[1].Headers.GetValues("Idempotency-Key").Single();

        Assert.NotEqual(firstKey, secondKey);
    }

    [Fact]
    public async Task SendAsync_ComDisableAutomaticIdempotencyKey_NaoAnexaNadaMesmoEmEndpointMutavel()
    {
        var options = new ParceleMaisOptions { ClientId = "id", ClientSecret = "secret" };
        options.Resilience.DisableAutomaticIdempotencyKey = true;

        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = BuildClient(inner, options);

        await client.PostAsync("v1/order", new StringContent("{}"));

        Assert.False(inner.Requests[0].Headers.Contains("Idempotency-Key"));
    }
}
