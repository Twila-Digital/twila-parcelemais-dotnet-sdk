using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using ParceleMais.Errors;
using ParceleMais.Http;

namespace ParceleMais.Resilience;

internal sealed class ResilienceDelegatingHandler(ResiliencePipeline<HttpResponseMessage> pipeline) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = ResilienceContextPool.Shared.Get(cancellationToken);
        context.Properties.Set(ResilienceContextRequestKey.Key, request);

        try
        {
            return await pipeline.ExecuteAsync(
                static async (ctx, state) =>
                {
                    var attempt = await state.Request.CloneAsync().ConfigureAwait(false);
                    return await state.Handler.SendAsyncCore(attempt, ctx.CancellationToken).ConfigureAwait(false);
                },
                context,
                (Handler: this, Request: request)).ConfigureAwait(false);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new ParceleMaisTimeoutException("A requisição excedeu o tempo limite configurado.", ex);
        }
        catch (BrokenCircuitException ex)
        {
            throw new ParceleMaisTimeoutException("O circuit breaker está aberto — chamadas recentes falharam de forma consistente.", ex);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private Task<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken) =>
        base.SendAsync(request, cancellationToken);
}
