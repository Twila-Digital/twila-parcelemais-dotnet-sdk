namespace ParceleMais.Resilience;

internal static class IdempotencyClassifier
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private static readonly string[] MutablePathsRequiringIdempotencyKey =
    [
        "v1/order/start-cdc-sale",
        "v1/order/invoice",
        "v1/order",
        "v1/webhooks"
    ];

    public static bool IsRetrySafe(HttpRequestMessage request)
    {
        if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head || request.Method == HttpMethod.Options)
            return true;

        var path = request.RequestUri?.AbsolutePath.TrimStart('/') ?? string.Empty;

        if (request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete)
            return path.StartsWith("v1/webhooks/", StringComparison.OrdinalIgnoreCase);

        if (request.Method != HttpMethod.Post)
            return false;

        if (path.EndsWith("v1/authentication/accesstoken", StringComparison.OrdinalIgnoreCase))
            return true;

        var isMutableEndpoint = Array.Exists(MutablePathsRequiringIdempotencyKey,
            mutablePath => path.EndsWith(mutablePath, StringComparison.OrdinalIgnoreCase));

        return isMutableEndpoint && request.Headers.Contains(IdempotencyKeyHeader);
    }
}
