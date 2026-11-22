using Polly;
using System.Threading.RateLimiting;

namespace MusicSearcher.Http;

/// <summary>
/// Retries and request limits of the HTTP clients of the music services. Limits: docs/external-services.md.
/// </summary>
public sealed class MusicServiceHttpOptions
{
    /// <summary>
    /// A busy MusicBrainz sheds each request on its own, in the evening about every second one, without long busy
    /// windows (measured in #93): more attempts one second apart get through sooner than long delays, and our limiter
    /// keeps them at one request per second. It answers in 0.05 to 0.6 seconds, about 1.4 on a new connection, so an
    /// attempt gets 3 seconds. Three attempts in about 2 seconds, 4 at most; then <c>MusicSearcherClient</c> asks
    /// Last.fm, and MusicBrainz once more only when Last.fm does not help (#91).
    /// </summary>
    public ServiceCallPolicy MusicBrainz { get; init; } = new(
        MaxRetryAttempts: 2,
        RetryDelay: TimeSpan.FromSeconds(1),
        BackoffType: DelayBackoffType.Constant,
        AttemptTimeout: TimeSpan.FromSeconds(3),
        TotalTimeout: TimeSpan.FromSeconds(4));

    /// <summary>
    /// Setlist.fm answers in under a second (the setlists screen took 0.8 seconds on the dev bot): retries after 2 and
    /// 4 seconds, 5 seconds per attempt, 10 seconds in total.
    /// </summary>
    public ServiceCallPolicy SetlistFm { get; init; } = new(
        MaxRetryAttempts: 2,
        RetryDelay: TimeSpan.FromSeconds(2),
        BackoffType: DelayBackoffType.Exponential,
        AttemptTimeout: TimeSpan.FromSeconds(5),
        TotalTimeout: TimeSpan.FromSeconds(10));

    public RateLimiter MusicBrainzRateLimiter { get; init; } = CreateRateLimiter(requestsPerSecond: 1);

    public RateLimiter SetlistFmRateLimiter { get; init; } = CreateRateLimiter(requestsPerSecond: 2);

    private static TokenBucketRateLimiter CreateRateLimiter(int requestsPerSecond)
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = requestsPerSecond,
            TokensPerPeriod = requestsPerSecond,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueLimit = 20,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }
}
