using Microsoft.Extensions.DependencyInjection;
using MusicSearcher.Http;
using Polly;
using Polly.Timeout;
using Shouldly;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.RateLimiting;

namespace MusicSearcher.Tests.Http;

public class MusicServiceHttpClientsTest
{
    private const int Retries = 2;
    private const string Url = "https://musicbrainz.org/ws/2/artist";
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task MusicBrainz_TemporaryError_RetriesAndSucceeds(HttpStatusCode status)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(status), () => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler);

        // Act
        var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        handler.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task MusicBrainz_AlwaysBusy_SixAttemptsOneSecondApart()
    {
        // Arrange: the default delays; the fake clock records them and lets them pass at once.
        var time = new RecordingTimeProvider();
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler, defaultPolicy: true, time: time);

        // Act
        var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Calls.ShouldBe(6);
        time.Delays.ShouldBe(Enumerable.Repeat(TimeSpan.FromSeconds(1), 5));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    public async Task MusicBrainz_BusyWithRetryAfter_StillWaitsOneSecond(string retryAfter)
    {
        // Arrange: a busy MusicBrainz sends Retry-After; following it made the retries come at once (seen on the dev bot).
        var time = new RecordingTimeProvider();
        var handler = new FakeHttpMessageHandler(() =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
            return response;
        });
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler, defaultPolicy: true, time: time);

        // Act
        await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        handler.Calls.ShouldBe(6);
        time.Delays.ShouldBe(Enumerable.Repeat(TimeSpan.FromSeconds(1), 5));
    }

    [Fact]
    public async Task SetlistFm_AlwaysFailing_ThreeAttemptsAfterTwoAndFourSeconds()
    {
        // Arrange
        var time = new RecordingTimeProvider();
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(MusicServiceHttpClients.SetlistFmClientName, handler, defaultPolicy: true, time: time);

        // Act
        await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        handler.Calls.ShouldBe(3);
        time.Delays.ShouldBe(new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) });
    }

    [Theory]
    [InlineData(MusicServiceHttpClients.MusicBrainzClientName, 8, 3)]
    [InlineData(MusicServiceHttpClients.SetlistFmClientName, 10, 5)]
    public async Task Request_DefaultPolicy_TotalAndAttemptTimeouts(string name, int totalSeconds, int attemptSeconds)
    {
        // Arrange
        var time = new RecordingTimeProvider();
        var client = CreateClient(name, new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK)), defaultPolicy: true, time: time);

        // Act
        await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert: the whole call first, then the attempt inside it.
        time.Timeouts.ShouldBe(new[] { TimeSpan.FromSeconds(totalSeconds), TimeSpan.FromSeconds(attemptSeconds) });
    }

    [Fact]
    public async Task MusicBrainz_ConnectionReset_RetriesAndSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => throw new HttpRequestException("Connection reset"), () => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler);

        // Act
        var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        handler.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task MusicBrainz_AlwaysBusy_StopsAfterRetriesAndReturnsLastResponse()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler);

        // Act
        var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Calls.ShouldBe(1 + Retries);
    }

    [Fact]
    public async Task MusicBrainz_BadRequest_IsNotRetried()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler);

        // Act
        var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task MusicBrainz_Request_HasApplicationUserAgentAndBaseAddress()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler);

        // Act
        await client.GetAsync("artist/1", TestContext.Current.CancellationToken);

        // Assert
        handler.LastRequest!.RequestUri.ShouldBe(new Uri("https://musicbrainz.org/ws/2/artist/1"));
        var userAgent = handler.LastRequest.Headers.UserAgent.ToString();
        userAgent.ShouldStartWith("ConcertBuddy/");
        userAgent.ShouldContain("( https://github.com/skuill/ConcertBuddy )");
    }

    [Fact]
    public async Task MusicBrainz_NoTokenLeft_NextRequestWaitsForTheLimiter()
    {
        // Arrange
        using var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
            AutoReplenishment = false,
            QueueLimit = 1,
        });
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler, musicBrainzLimiter: limiter);
        await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Act
        var second = client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        second.IsCompleted.ShouldBeFalse();
        handler.Calls.ShouldBe(1);

        limiter.TryReplenish().ShouldBeTrue();
        await second.WaitAsync(SafetyTimeout, TestContext.Current.CancellationToken);
        handler.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task MusicBrainz_ServiceHangs_EveryAttemptTimesOut()
    {
        // Arrange
        var handler = new HangingHandler();
        var client = CreateClient(MusicServiceHttpClients.MusicBrainzClientName, handler, attemptTimeout: TimeSpan.FromMilliseconds(20));

        // Act, Assert
        await Should.ThrowAsync<TimeoutRejectedException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));
        handler.Calls.ShouldBe(1 + Retries);
    }

    [Theory]
    [InlineData(MusicServiceHttpClients.MusicBrainzClientName)]
    [InlineData(MusicServiceHttpClients.SetlistFmClientName)]
    public async Task Request_ServiceHangs_TheTotalTimeoutEndsTheCallWithoutRetries(string name)
    {
        // Arrange: the attempt would wait much longer, so only the total timeout can end it, and nothing retries after it.
        var handler = new HangingHandler();
        var client = CreateClient(name, handler, attemptTimeout: TimeSpan.FromMinutes(1), totalTimeout: TimeSpan.FromMilliseconds(50));

        // Act, Assert
        await Should.ThrowAsync<TimeoutRejectedException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken).WaitAsync(SafetyTimeout, TestContext.Current.CancellationToken));
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task SetlistFm_TooManyRequests_RetriesAndSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests), () => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(MusicServiceHttpClients.SetlistFmClientName, handler);

        // Act
        var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        handler.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task SetlistFm_NotFound_ThrowsWithStatusWithoutRetries()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(MusicServiceHttpClients.SetlistFmClientName, handler);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task SetlistFm_AlwaysFailing_ThrowsAfterRetries()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = CreateClient(MusicServiceHttpClients.SetlistFmClientName, handler);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(Url, TestContext.Current.CancellationToken));

        // Assert
        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        handler.Calls.ShouldBe(1 + Retries);
    }

    internal static HttpClient CreateClient(string name, HttpMessageHandler handler, RateLimiter? musicBrainzLimiter = null, TimeSpan? attemptTimeout = null, bool defaultPolicy = false, TimeProvider? time = null, Action<IServiceCollection>? configure = null, TimeSpan? totalTimeout = null)
    {
        var services = new ServiceCollection();
        var defaults = new MusicServiceHttpOptions();
        if (time != null)
        {
            services.AddSingleton(time);
        }
        configure?.Invoke(services);
        // Without the default policy: retries at once, and timeouts that do not fire unless a test sets them.
        var fast = new ServiceCallPolicy(Retries, TimeSpan.Zero, DelayBackoffType.Constant, attemptTimeout ?? TimeSpan.FromSeconds(15), totalTimeout ?? TimeSpan.FromSeconds(60));
        services.AddMusicServiceHttpClients(new MusicServiceHttpOptions
        {
            MusicBrainz = defaultPolicy ? defaults.MusicBrainz : fast,
            SetlistFm = defaultPolicy ? defaults.SetlistFm : fast,
            MusicBrainzRateLimiter = musicBrainzLimiter ?? CreateOpenLimiter(),
            SetlistFmRateLimiter = CreateOpenLimiter(),
        });
        services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(name);
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        private int _calls;

        public int Calls => _calls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Not reachable");
        }
    }

    /// <summary>
    /// Records the retry delays and lets them pass at once. The timeouts keep their real timers, so they never fire in a
    /// test, and are recorded when they are set: a timeout gets an infinite timer first and its time with Change.
    /// </summary>
    private sealed class RecordingTimeProvider : TimeProvider
    {
        public ConcurrentQueue<TimeSpan> Delays { get; } = new();

        public ConcurrentQueue<TimeSpan> Timeouts { get; } = new();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                return new TimeoutTimer(System.CreateTimer(callback, state, dueTime, period), Timeouts);
            }
            Delays.Enqueue(dueTime);
            return System.CreateTimer(callback, state, TimeSpan.Zero, period);
        }

        private sealed class TimeoutTimer : ITimer
        {
            private readonly ITimer _timer;
            private readonly ConcurrentQueue<TimeSpan> _timeouts;

            public TimeoutTimer(ITimer timer, ConcurrentQueue<TimeSpan> timeouts)
            {
                _timer = timer;
                _timeouts = timeouts;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (dueTime != Timeout.InfiniteTimeSpan)
                {
                    _timeouts.Enqueue(dueTime);
                }
                return _timer.Change(dueTime, period);
            }

            public void Dispose() => _timer.Dispose();

            public ValueTask DisposeAsync() => _timer.DisposeAsync();
        }
    }

    private static TokenBucketRateLimiter CreateOpenLimiter()
    {
        return new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1000,
            TokensPerPeriod = 1000,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueLimit = 0,
        });
    }
}
