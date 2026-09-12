using System.Net;
using ParceleMais.Configuration;
using ParceleMais.Errors;
using ParceleMais.Resilience;
using ParceleMais.UnitTests.TestUtilities;

namespace ParceleMais.UnitTests.Resilience;

public class ResilienceDelegatingHandlerTests
{
    private static ParceleMaisResilienceOptions FastOptions() => new()
    {
        TotalTimeout = TimeSpan.FromSeconds(5),
        AttemptTimeout = TimeSpan.FromSeconds(2),
        MaxRetryAttempts = 3,
        RetryBaseDelay = TimeSpan.FromMilliseconds(5),
        CircuitBreakerMinimumThroughput = 10,
        CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(10),
        CircuitBreakerBreakDuration = TimeSpan.FromMilliseconds(500)
    };

    private static HttpClient BuildClient(FakeHttpMessageHandler inner, ParceleMaisResilienceOptions? options = null)
    {
        var pipeline = ResiliencePipelineFactory.Create(options ?? FastOptions());
        var handler = new ResilienceDelegatingHandler(pipeline) { InnerHandler = inner };
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.parcelemais.com.br/integration/") };
    }

    [Fact]
    public async Task SendAsync_GetCom503_TentaNovamenteAteSucesso()
    {
        var inner = new FakeHttpMessageHandler((_, callIndex) => Task.FromResult(
            new HttpResponseMessage(callIndex < 3 ? (HttpStatusCode)503 : HttpStatusCode.OK)));

        var client = BuildClient(inner);
        var response = await client.GetAsync("v1/order/123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_PostSemIdempotencyKeyCom503_NaoTentaNovamente()
    {
        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)503)));

        var client = BuildClient(inner);
        var response = await client.PostAsync("v1/order", new StringContent("{}"));

        Assert.Equal((HttpStatusCode)503, response.StatusCode);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_PostComIdempotencyKeyCom503_TentaNovamente()
    {
        var inner = new FakeHttpMessageHandler((_, callIndex) => Task.FromResult(
            new HttpResponseMessage(callIndex < 2 ? (HttpStatusCode)503 : HttpStatusCode.OK)));

        var client = BuildClient(inner);
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/order") { Content = new StringContent("{}") };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_Com500_NaoTentaNovamentePorPadrao()
    {
        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var client = BuildClient(inner);
        var response = await client.GetAsync("v1/order/123");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_Com500EComRetryOn500Habilitado_TentaNovamente()
    {
        var options = FastOptions();
        options.RetryOn500 = true;

        var inner = new FakeHttpMessageHandler((_, callIndex) => Task.FromResult(
            new HttpResponseMessage(callIndex < 2 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK)));

        var client = BuildClient(inner, options);
        var response = await client.GetAsync("v1/order/123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_Com429ComRetryAfter_RespeitaODelayInformado()
    {
        var retryAfterSeconds = 0;
        var inner = new FakeHttpMessageHandler((_, callIndex) =>
        {
            if (callIndex == 1)
            {
                var response = new HttpResponseMessage((HttpStatusCode)429);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(20));
                retryAfterSeconds++;
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var client = BuildClient(inner);
        var response = await client.GetAsync("v1/order/123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, retryAfterSeconds);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_ComFalhasConsecutivasAcimaDoLimite_AbreOCircuitBreaker()
    {
        var options = FastOptions();
        options.MaxRetryAttempts = 1;
        options.CircuitBreakerMinimumThroughput = 2;
        options.CircuitBreakerFailureRatio = 0.5;
        options.CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);

        var inner = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)503)));
        var client = BuildClient(inner, options);

        await client.GetAsync("v1/order/1");
        await client.GetAsync("v1/order/2");

        var callsBeforeOpen = inner.CallCount;

        await Assert.ThrowsAsync<ParceleMaisTimeoutException>(() => client.GetAsync("v1/order/3"));

        Assert.Equal(callsBeforeOpen, inner.CallCount);
    }

    [Fact]
    public async Task SendAsync_ComTimeoutDeTentativaExcedido_LancaParceleMaisTimeoutException()
    {
        var options = FastOptions();
        options.AttemptTimeout = TimeSpan.FromMilliseconds(20);
        options.MaxRetryAttempts = 1;

        var inner = new FakeHttpMessageHandler(async (_, _, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = BuildClient(inner, options);

        await Assert.ThrowsAsync<ParceleMaisTimeoutException>(() => client.GetAsync("v1/order/1"));
    }

    [Fact]
    public async Task SendAsync_ComCancellationTokenCancelado_PropagaOCancelamento()
    {
        using var cts = new CancellationTokenSource();
        var inner = new FakeHttpMessageHandler(async (_, _) =>
        {
            cts.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var client = BuildClient(inner);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("v1/order/1", cts.Token));
    }
}
