using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher.Http;
using Shouldly;
using System.Net;

namespace MusicSearcher.Tests.Http;

public class MusicBrainzBusyLoggerTest
{
    private const string SearchUrl = "https://musicbrainz.org/ws/2/artist?query=artist:%22metallica%22&limit=5&offset=0&fmt=json";

    private readonly ListLogger<MusicBrainzBusyLogger> _logger = new();
    private readonly ManualTimeProvider _time = new();

    [Fact]
    public async Task Send_BusyAfterAnAnswer_WarningWithTheHeadersAndTheTimeSinceThePreviousRequestButNoQuery()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK), () => Busy("search", "search-shed", "1"));
        using var invoker = CreateInvoker(handler);
        await SendAsync(invoker);
        _time.Advance(TimeSpan.FromSeconds(12.5));

        // Act
        var response = await SendAsync(invoker);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        handler.Calls.ShouldBe(2);
        Logged().ShouldBe(new[]
        {
            (LogLevel.Warning, "MusicBrainz answered 503 to /ws/2/artist: zone search, who search-shed, Retry-After 1, seconds since the previous request 12.5"),
        });
    }

    [Fact]
    public async Task Send_FirstRequestBusyWithoutHeaders_DashesInsteadOfTheValues()
    {
        // Arrange
        using var invoker = CreateInvoker(new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        // Act
        await SendAsync(invoker, "https://musicbrainz.org/ws/2/artist/65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab?inc=url-rels");

        // Assert
        Logged().ShouldBe(new[]
        {
            (LogLevel.Warning, "MusicBrainz answered 503 to /ws/2/artist/65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab: zone -, who -, Retry-After -, seconds since the previous request -"),
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task Send_OtherBusyStatus_Warning(HttpStatusCode status)
    {
        // Arrange
        using var invoker = CreateInvoker(new FakeHttpMessageHandler(() => new HttpResponseMessage(status)));

        // Act
        await SendAsync(invoker);

        // Assert
        Logged().Single().ShouldBe((LogLevel.Warning, $"MusicBrainz answered {(int)status} to /ws/2/artist: zone -, who -, Retry-After -, seconds since the previous request -"));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task Send_NotBusy_NothingLogged(HttpStatusCode status)
    {
        // Arrange
        using var invoker = CreateInvoker(new FakeHttpMessageHandler(() => new HttpResponseMessage(status)));

        // Act
        await SendAsync(invoker);

        // Assert
        Logged().ShouldBeEmpty();
    }

    [Fact]
    public async Task MusicBrainzClient_AlwaysBusy_EveryAttemptIsLogged()
    {
        // Arrange: the handler is inside the retries of the MusicBrainz client.
        var handler = new FakeHttpMessageHandler(() => Busy("global", "global", "0"));
        var client = MusicServiceHttpClientsTest.CreateClient(
            MusicServiceHttpClients.MusicBrainzClientName,
            handler,
            configure: services => services.AddSingleton<ILogger<MusicBrainzBusyLogger>>(_logger));

        // Act
        await client.GetAsync(SearchUrl, TestContext.Current.CancellationToken);

        // Assert
        handler.Calls.ShouldBe(3);
        Logged().Select(x => x.Message[..x.Message.IndexOf(", seconds", StringComparison.Ordinal)]).ShouldBe(
            Enumerable.Repeat("MusicBrainz answered 503 to /ws/2/artist: zone global, who global, Retry-After 0", 3));
    }

    [Fact]
    public async Task SetlistFmClient_Busy_NotLoggedHere()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => Busy("global", "global", "0"));
        var client = MusicServiceHttpClientsTest.CreateClient(
            MusicServiceHttpClients.SetlistFmClientName,
            handler,
            configure: services => services.AddSingleton<ILogger<MusicBrainzBusyLogger>>(_logger));

        // Act
        await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync(SearchUrl, TestContext.Current.CancellationToken));

        // Assert
        Logged().ShouldBeEmpty();
    }

    private HttpMessageInvoker CreateInvoker(HttpMessageHandler inner)
    {
        return new HttpMessageInvoker(new MusicBrainzBusyLogger(_logger, _time) { InnerHandler = inner });
    }

    private static Task<HttpResponseMessage> SendAsync(HttpMessageInvoker invoker, string url = SearchUrl)
    {
        return invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), TestContext.Current.CancellationToken);
    }

    private static HttpResponseMessage Busy(string zone, string who, string retryAfter)
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryAddWithoutValidation("X-RateLimit-Zone", zone);
        response.Headers.TryAddWithoutValidation("X-RateLimit-Who", who);
        response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        return response;
    }

    private List<(LogLevel Level, string Message)> Logged()
    {
        return _logger.Entries.ToList();
    }

    /// <summary>
    /// A clock that moves only when the test says so.
    /// </summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _now = TimeSpan.TicksPerHour;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _now;

        public void Advance(TimeSpan time) => _now += time.Ticks;
    }
}
