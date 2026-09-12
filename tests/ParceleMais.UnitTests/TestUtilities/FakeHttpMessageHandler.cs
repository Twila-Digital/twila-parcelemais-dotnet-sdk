namespace ParceleMais.UnitTests.TestUtilities;

internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, int, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    private int _callCount;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, int, Task<HttpResponseMessage>> respond)
        : this((request, callIndex, _) => respond(request, callIndex))
    {
    }

    public int CallCount => _callCount;
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var callIndex = Interlocked.Increment(ref _callCount);
        lock (Requests)
            Requests.Add(request);

        return await respond(request, callIndex, cancellationToken).ConfigureAwait(false);
    }
}
