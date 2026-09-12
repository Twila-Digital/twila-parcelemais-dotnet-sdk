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
            var executeTask = pipeline.ExecuteAsync(
                static async (ctx, state) =>
                {
                    var attempt = await state.Request.CloneAsync().ConfigureAwait(false);
                    return await state.Handler.SendAsyncCore(attempt, ctx.CancellationToken).ConfigureAwait(false);
                },
                context,
                (Handler: this, Request: request)).AsTask();

            return await WaitAsync(executeTask, cancellationToken).ConfigureAwait(false);
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

    private static async Task<T> WaitAsync<T>(Task<T> task, CancellationToken cancellationToken)
    {
        if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            return await task.ConfigureAwait(false);

        var cancellationTcs = new TaskCompletionSource<object?>();

        using (cancellationToken.Register(static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), cancellationTcs))
        {
            var completed = await Task.WhenAny(task, cancellationTcs.Task).ConfigureAwait(false);

            if (completed == cancellationTcs.Task)
                cancellationToken.ThrowIfCancellationRequested();
        }

        return await task.ConfigureAwait(false);
    }
}
