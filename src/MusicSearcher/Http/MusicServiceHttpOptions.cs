using System.Threading.RateLimiting;

namespace MusicSearcher.Http;

/// <summary>
/// Retries and request limits of the HTTP clients of the music services. Limits: docs/external-services.md.
/// </summary>
public sealed class MusicServiceHttpOptions
{
    /// <summary>
    /// A busy MusicBrainz sheds each request on its own, in the evening about every second one, without long busy
    /// windows (measured in #93): more attempts one second apart get through sooner than long delays. Six attempts take
    /// about 6 seconds, and our limiter keeps them at one request per second.
    /// </summary>
    public int MusicBrainzMaxRetryAttempts { get; init; } = 5;

    public TimeSpan MusicBrainzRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Setlist.fm: retries after 2, 4 and 8 seconds.
    /// </summary>
    public int SetlistFmMaxRetryAttempts { get; init; } = 3;

    public TimeSpan SetlistFmRetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(15);

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
