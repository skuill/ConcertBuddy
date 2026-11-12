using System.Threading.RateLimiting;

namespace MusicSearcher.Http;

/// <summary>
/// Retries and request limits of the HTTP clients of the music services. Limits: docs/external-services.md.
/// </summary>
public sealed class MusicServiceHttpOptions
{
    public int MaxRetryAttempts { get; init; } = 3;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);

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
