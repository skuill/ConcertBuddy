using FakeItEasy;
using Microsoft.Extensions.Logging;
using MusicSearcher.Http;
using MusicSearcher.MusicService.SetlistFm;
using MusicSearcher.Tests.Http;
using SetlistNet;
using Shouldly;
using System.Net;
using System.Text;

namespace MusicSearcher.Tests.MusicService.SetlistFm;

public class SetlistFmServiceClientTest
{
    private const string Mbid = "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab";

    private const string SetlistJson = """{"id":"4b48efd6","eventDate":"31-10-2026","artist":{"mbid":"65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab","name":"Metallica"},"venue":{"name":"Sphere"},"sets":{"set":[]}}""";

    private readonly ILogger<SetlistFmServiceClient> _logger = A.Fake<ILogger<SetlistFmServiceClient>>();

    [Fact]
    public async Task SearchArtistSetlists_Found_ReturnsSetlists()
    {
        // Arrange
        var client = CreateClient(() => Json($$"""{"type":"setlists","itemsPerPage":20,"page":2,"total":851,"setlist":[{{SetlistJson}}]}"""));

        // Act
        var result = await client.SearchArtistSetlists(Mbid, 2);

        // Assert
        result.Total.ShouldBe(851);
        result.Page.ShouldBe(2);
        result.ItemsPerPage.ShouldBe(20);
        result.Setlist.Single().Id.ShouldBe("4b48efd6");
        result.Setlist.Single().EventDate.ShouldBe("31-10-2026");
        result.Setlist.Single().ArtistName.ShouldBe("Metallica");
        AssertLogged(LogLevel.Error, times: 0);
    }

    [Fact]
    public async Task SearchArtistSetlists_NotFound_ReturnsNullWithoutError()
    {
        // Arrange
        var client = CreateClient(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await client.SearchArtistSetlists(Mbid);

        // Assert
        result.ShouldBeNull();
        AssertLogged(LogLevel.Information, times: 1);
        AssertLogged(LogLevel.Error, times: 0);
    }

    [Fact]
    public async Task SearchArtistSetlists_ServiceFails_ReturnsNullAndLogsError()
    {
        // Arrange
        var client = CreateClient(() => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var result = await client.SearchArtistSetlists(Mbid);

        // Assert
        result.ShouldBeNull();
        AssertLogged(LogLevel.Error, times: 1);
    }

    [Fact]
    public async Task SearchSetlist_Found_ReturnsSetlist()
    {
        // Arrange
        var client = CreateClient(() => Json(SetlistJson));

        // Act
        var result = await client.SearchSetlist("4b48efd6");

        // Assert
        result.Id.ShouldBe("4b48efd6");
        result.VenueName.ShouldBe("Sphere");
    }

    [Fact]
    public async Task SearchSetlist_ErrorAnswer_ReturnsNullInsteadOfEmptySetlist()
    {
        // Arrange
        var client = CreateClient(() => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("""{"code":429,"status":"Too Many Requests"}""") });

        // Act
        var result = await client.SearchSetlist("4b48efd6");

        // Assert
        result.ShouldBeNull();
        AssertLogged(LogLevel.Error, times: 1);
    }

    private SetlistFmServiceClient CreateClient(Func<HttpResponseMessage> response)
    {
        var httpClient = MusicServiceHttpClientsTest.CreateClient(MusicServiceHttpClients.SetlistFmClientName, new FakeHttpMessageHandler(response));
        return new SetlistFmServiceClient(_logger, new SetlistApi("key", httpClient: httpClient));
    }

    private void AssertLogged(LogLevel level, int times)
    {
        var calls = A.CallTo(_logger).Where(call => call.Method.Name == nameof(ILogger.Log) && call.GetArgument<LogLevel>(0) == level);
        if (times == 0)
        {
            calls.MustNotHaveHappened();
        }
        else
        {
            calls.MustHaveHappened(times, Times.Exactly);
        }
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
