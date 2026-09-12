using Polly;

namespace ParceleMais.Resilience;

internal static class ResilienceContextRequestKey
{
    public static readonly ResiliencePropertyKey<HttpRequestMessage> Key = new("ParceleMais.HttpRequestMessage");
}
