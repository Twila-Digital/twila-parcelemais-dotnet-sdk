using ParceleMais.Configuration;

namespace ParceleMais.Idempotency;

internal sealed class IdempotencyKeyDelegatingHandler(ParceleMaisOptions options) : DelegatingHandler
{
    private const string HeaderName = "Idempotency-Key";

    private static readonly string[] EndpointsRequiringIdempotencyKey =
    [
        "v1/order/start-cdc-sale",
        "v1/order/invoice",
        "v1/order",
        "v1/webhooks"
    ];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!options.Resilience.DisableAutomaticIdempotencyKey && ShouldAttachIdempotencyKey(request))
            request.Headers.TryAddWithoutValidation(HeaderName, Guid.NewGuid().ToString());

        return base.SendAsync(request, cancellationToken);
    }

    private static bool ShouldAttachIdempotencyKey(HttpRequestMessage request)
    {
        if (request.Method != HttpMethod.Post)
            return false;

        var path = request.RequestUri?.AbsolutePath.TrimStart('/') ?? string.Empty;

        return Array.Exists(EndpointsRequiringIdempotencyKey, endpoint => path.EndsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }
}
