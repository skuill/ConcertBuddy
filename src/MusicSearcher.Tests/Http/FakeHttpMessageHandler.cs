namespace MusicSearcher.Tests.Http;

/// <summary>
/// Answers with the given responses in order and remembers the requests. The last response repeats.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpResponseMessage>[] _responses;
    private int _calls;

    public FakeHttpMessageHandler(params Func<HttpResponseMessage>[] responses)
    {
        _responses = responses;
    }

    public int Calls => _calls;

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var index = Math.Min(Interlocked.Increment(ref _calls), _responses.Length) - 1;
        return Task.FromResult(_responses[index]());
    }
}
