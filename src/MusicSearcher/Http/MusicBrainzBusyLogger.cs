using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;

namespace MusicSearcher.Http;

/// <summary>
/// Logs every busy answer of MusicBrainz with the headers that say why (#93), one line per attempt: it sits inside the
/// retries. A busy MusicBrainz sheds the load of all clients (<c>X-RateLimit-Zone: global</c> or <c>search</c>,
/// <c>X-RateLimit-Who: search-shed</c>), which is not the same as our own rate.
/// </summary>
internal sealed class MusicBrainzBusyLogger : DelegatingHandler
{
    private const long NoRequestYet = 0;

    private readonly ILogger<MusicBrainzBusyLogger> _logger;
    private readonly TimeProvider _timeProvider;

    // One handler for the whole run: MusicBrainzClient keeps its HttpClient as long as the bot works.
    private long _previousRequest = NoRequestYet;

    public MusicBrainzBusyLogger(ILogger<MusicBrainzBusyLogger> logger, TimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetTimestamp();
        var previous = Interlocked.Exchange(ref _previousRequest, now);
        var response = await base.SendAsync(request, cancellationToken);
        if (IsBusy(response.StatusCode))
        {
            // The path only: the query string holds what the user typed.
            _logger.LogWarning(
                "MusicBrainz answered {StatusCode} to {Path}: zone {Zone}, who {Who}, Retry-After {RetryAfter}, seconds since the previous request {SincePreviousSeconds}",
                (int)response.StatusCode,
                request.RequestUri?.AbsolutePath,
                Header(response, "X-RateLimit-Zone"),
                Header(response, "X-RateLimit-Who"),
                Header(response, "Retry-After"),
                previous == NoRequestYet ? "-" : _timeProvider.GetElapsedTime(previous, now).TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture));
        }
        return response;
    }

    private static bool IsBusy(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : "-";
    }
}
