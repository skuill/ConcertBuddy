using Microsoft.Extensions.DependencyInjection;
using MusicSearcher.Http;
using Polly.Timeout;
using Shouldly;
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

    internal static HttpClient CreateClient(string name, HttpMessageHandler handler, RateLimiter? musicBrainzLimiter = null, TimeSpan? attemptTimeout = null)
    {
        var services = new ServiceCollection();
        services.AddMusicServiceHttpClients(new MusicServiceHttpOptions
        {
            MaxRetryAttempts = Retries,
            RetryDelay = TimeSpan.Zero,
            AttemptTimeout = attemptTimeout ?? TimeSpan.FromSeconds(15),
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
