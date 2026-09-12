using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using ParceleMais.Configuration;

namespace ParceleMais.Resilience;

internal static class ResiliencePipelineFactory
{
    public static ResiliencePipeline<HttpResponseMessage> Create(ParceleMaisResilienceOptions options)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = options.TotalTimeout
        });

        if (options.MaxRetryAttempts > 1)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = options.MaxRetryAttempts - 1,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = options.RetryBaseDelay,
                ShouldHandle = args => new ValueTask<bool>(ShouldRetry(args, options)),
                DelayGenerator = args => new ValueTask<TimeSpan?>(GetRetryDelay(args))
            });
        }

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = options.CircuitBreakerFailureRatio,
            SamplingDuration = options.CircuitBreakerSamplingDuration,
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            BreakDuration = options.CircuitBreakerBreakDuration,
            ShouldHandle = args => new ValueTask<bool>(IsTransientFailure(args.Outcome, options))
        });

        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = options.AttemptTimeout
        });

        return builder.Build();
    }

    private static bool ShouldRetry(RetryPredicateArguments<HttpResponseMessage> args, ParceleMaisResilienceOptions options)
    {
        if (!args.Context.Properties.TryGetValue(ResilienceContextRequestKey.Key, out var request) || request is null)
            return false;

        if (!IdempotencyClassifier.IsRetrySafe(request))
            return false;

        return IsTransientFailure(args.Outcome, options);
    }

    private static bool IsTransientFailure(Outcome<HttpResponseMessage> outcome, ParceleMaisResilienceOptions options)
    {
        if (outcome.Exception is HttpRequestException or TimeoutRejectedException)
            return true;

        if (outcome.Result is null)
            return false;

        var statusCode = (int)outcome.Result.StatusCode;

        if (statusCode is 408 or 429 or 502 or 503 or 504)
            return true;

        return statusCode == 500 && options.RetryOn500;
    }

    private static TimeSpan? GetRetryDelay(RetryDelayGeneratorArguments<HttpResponseMessage> args) =>
        args.Outcome.Result?.Headers.RetryAfter?.Delta;
}
