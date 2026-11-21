using Polly;

namespace MusicSearcher.Http;

/// <summary>
/// How long the bot waits for one music service: the retries, one attempt and the whole call with its retries and the
/// time in the queue of the rate limiter. The total is the time a user waits at most (#91).
/// </summary>
public sealed record ServiceCallPolicy(
    int MaxRetryAttempts,
    TimeSpan RetryDelay,
    DelayBackoffType BackoffType,
    TimeSpan AttemptTimeout,
    TimeSpan TotalTimeout);
