using FakeItEasy;
using Hqub.MusicBrainz;
using HqubArtist = Hqub.Lastfm.Entities.Artist;
using LyricsScraperNET;
using LyricsScraperNET.Models.Requests;
using LyricsScraperNET.Models.Responses;
using Microsoft.Extensions.Logging;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.LastFm;
using MusicSearcher.MusicService;
using MusicSearcher.MusicService.Abstract;
using MusicSearcher.MusicService.SetlistFm;
using MusicSearcher.Tests.Http;
using Shouldly;
using System.Net;

namespace MusicSearcher.Tests;

public class MusicSearcherClientTest
{
    private const string Artist = "Metallica";
    private const string Song = "One";
    private const string Mbid = "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab";

    private readonly ILyricsScraperClient _lyricsClient = A.Fake<ILyricsScraperClient>();
    private readonly ISetlistFmServiceClient _setlistClient = A.Fake<ISetlistFmServiceClient>();
    private readonly ILogger<MusicSearcherClient> _logger = A.Fake<ILogger<MusicSearcherClient>>();

    [Fact]
    public async Task SearchTrack_TwoServices_CombinesTheirResults()
    {
        // Arrange
        var yandex = CreateService(CreateTrack(MusicServiceType.Yandex, "One", downloadLink: "https://audio"));
        var lastFm = CreateService(CreateTrack(MusicServiceType.LastFm, "One (Live)", externalLink: "https://last.fm"));
        var client = CreateClient(yandex, lastFm);

        // Act
        var result = (MusicTrack)await client.SearchTrack(Artist, Song);

        // Assert
        result.Count().ShouldBe(2);
        result.TrackName.ShouldBe("One");
        result.DownloadLink.ShouldBe("https://audio");
        result.TrackExternalLink.ShouldBe("https://last.fm");
        A.CallTo(() => yandex.SearchTrack(Artist, Song)).MustHaveHappenedOnceExactly();
        A.CallTo(() => lastFm.SearchTrack(Artist, Song)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchTrack_ServiceHasNoTrack_EmptyAndInformationNotError()
    {
        // Arrange: a stage solo like "Joe Hahn Solo" is no track of a music service.
        var service = A.Fake<IMusicServiceClient>();
        A.CallTo(() => service.SearchTrack(A<string>._, A<string>._)).Returns((MusicTrackBase?)null);
        var client = CreateClient(service);

        // Act
        var result = (MusicTrack)await client.SearchTrack("Linkin Park", "Joe Hahn Solo");

        // Assert
        result.ShouldBeEmpty();
        result.DownloadLink.ShouldBeNull();
        AssertLogged(LogLevel.Information, 1, "Music service [");
        AssertLogged(LogLevel.Error, 0);
    }

    [Fact]
    public async Task SearchTrack_FirstServiceHasNoTrack_AsksTheNextOne()
    {
        // Arrange
        var empty = A.Fake<IMusicServiceClient>();
        A.CallTo(() => empty.SearchTrack(A<string>._, A<string>._)).Returns((MusicTrackBase?)null);
        var working = CreateService(CreateTrack(MusicServiceType.Yandex, "One", downloadLink: "https://audio"));
        var client = CreateClient(empty, working);

        // Act
        var result = (MusicTrack)await client.SearchTrack(Artist, Song);

        // Assert
        result.Count().ShouldBe(1);
        result.DownloadLink.ShouldBe("https://audio");
    }

    [Fact]
    public async Task SearchTrack_ServiceFails_UsesOtherServices()
    {
        // Arrange
        var failing = A.Fake<IMusicServiceClient>();
        A.CallTo(() => failing.SearchTrack(A<string>._, A<string>._)).ThrowsAsync(new InvalidOperationException("Service is down"));
        var notImplemented = A.Fake<IMusicServiceClient>();
        A.CallTo(() => notImplemented.SearchTrack(A<string>._, A<string>._)).ThrowsAsync(new NotImplementedException());
        var working = CreateService(CreateTrack(MusicServiceType.Yandex, "One"));
        var client = CreateClient(failing, notImplemented, working);

        // Act
        var result = (MusicTrack)await client.SearchTrack(Artist, Song);

        // Assert
        result.Count().ShouldBe(1);
        result.TrackName.ShouldBe("One");
        AssertLogged(LogLevel.Error, 1, "Can't get track from music service");
    }

    [Fact]
    public async Task SearchLyric_Found_ReturnsLyricAndSendsArtistAndSong()
    {
        // Arrange
        A.CallTo(() => _lyricsClient.SearchLyricAsync(A<SearchRequest>._, A<CancellationToken>._))
            .Returns(LyricsScraperModelFactory.SearchResult(SearchResultCode.Found, "Lyric text"));
        var client = CreateClient();

        // Act
        var result = await client.SearchLyric(Artist, Song);

        // Assert
        result.ShouldBeEquivalentTo(new MusicLyric { IsSuccessSearchResult = true, LyricText = "Lyric text" });
        A.CallTo(() => _lyricsClient.SearchLyricAsync(
            A<SearchRequest>.That.Matches(x => x is ArtistAndSongSearchRequest && ((ArtistAndSongSearchRequest)x).Artist == Artist && ((ArtistAndSongSearchRequest)x).Song == Song),
            A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchLyric_NotFound_IsNotSuccess()
    {
        // Arrange
        A.CallTo(() => _lyricsClient.SearchLyricAsync(A<SearchRequest>._, A<CancellationToken>._))
            .Returns(LyricsScraperModelFactory.SearchResult(SearchResultCode.NotFound));
        var client = CreateClient();

        // Act
        var result = await client.SearchLyric(Artist, Song);

        // Assert
        result.IsSuccessSearchResult.ShouldBeFalse();
    }

    [Fact]
    public async Task SearchLyric_NoResult_IsNotSuccess()
    {
        // Arrange
        A.CallTo(() => _lyricsClient.SearchLyricAsync(A<SearchRequest>._, A<CancellationToken>._))
            .Returns(Task.FromResult<SearchResult>(null!));
        var client = CreateClient();

        // Act
        var result = await client.SearchLyric(Artist, Song);

        // Assert
        result.ShouldBeEquivalentTo(MusicLyric.Empty());
    }

    [Fact]
    public async Task SearchArtistSetlists_Page_ReturnsResultOfSetlistClient()
    {
        // Arrange
        var setlists = new MusicSetlists { Total = 851, Page = 2 };
        A.CallTo(() => _setlistClient.SearchArtistSetlists(Mbid, 2)).Returns(setlists);
        var client = CreateClient();

        // Act
        var result = await client.SearchArtistSetlists(Mbid, 2);

        // Assert
        result.ShouldBeSameAs(setlists);
        A.CallTo(() => _setlistClient.SearchArtistSetlists(Mbid, 2)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchSetlist_Id_ReturnsResultOfSetlistClient()
    {
        // Arrange
        var setlist = new MusicSetlist { Id = "4b48efd6" };
        A.CallTo(() => _setlistClient.SearchSetlist("4b48efd6")).Returns(setlist);
        var client = CreateClient();

        // Act
        var result = await client.SearchSetlist("4b48efd6");

        // Assert
        result.ShouldBeSameAs(setlist);
        A.CallTo(() => _setlistClient.SearchSetlist("4b48efd6")).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SearchArtistsByName_MusicBrainzFails_Unavailable(HttpStatusCode status)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(status));
        var client = CreateClient(handler);

        // Act
        var exception = await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistsByName(UniqueName()));

        // Assert
        exception.Service.ShouldBe("MusicBrainz");
        // Without Last.fm: MusicBrainz, then MusicBrainz once more.
        handler.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task SearchArtistsByName_NetworkError_Unavailable()
    {
        // Arrange
        var client = CreateClient(new FakeHttpMessageHandler(() => throw new HttpRequestException("Connection reset")));

        // Act
        var exception = await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistsByName(UniqueName()));

        // Assert
        exception.InnerException.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SearchArtistsByName_SameNameAgain_FromTheCacheWithoutARequest()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(SearchAnswer);
        var client = CreateClient(handler);
        var first = await client.SearchArtistsByName(Artist);

        // Act
        var second = await client.SearchArtistsByName(Artist);

        // Assert
        handler.Calls.ShouldBe(1);
        first.Select(x => (x.MBID, x.Name)).ShouldBe(new (string?, string?)[] { (Mbid, Artist) });
        second.Select(x => (x.MBID, x.Name)).ShouldBe(new (string?, string?)[] { (Mbid, Artist) });
    }

    [Fact]
    public async Task SearchArtistsByName_BusyThenAgain_TheBusyAnswerIsNotCached()
    {
        // Arrange: both turns of the first search are busy.
        var busy = () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var handler = new FakeHttpMessageHandler(busy, busy, SearchAnswer);
        var client = CreateClient(handler);
        await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistsByName(Artist));

        // Act
        var result = await client.SearchArtistsByName(Artist);

        // Assert
        handler.Calls.ShouldBe(3);
        result.Select(x => (x.MBID, x.Name)).ShouldBe(new (string?, string?)[] { (Mbid, Artist) });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchArtistsByName_EmptyName_EmptyWithoutRequest(string name)
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(() => throw new InvalidOperationException("No request expected"));
        var client = CreateClient(handler);

        // Act
        var result = await client.SearchArtistsByName(name);

        // Assert
        result.ShouldBeEmpty();
        handler.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task SearchArtistByMBID_MusicBrainzHasNoSuchArtist_EmptyArtist(HttpStatusCode status)
    {
        // Arrange
        var client = CreateClient(new FakeHttpMessageHandler(() => new HttpResponseMessage(status)));

        // Act
        var result = await client.SearchArtistByMBID(Guid.NewGuid().ToString());

        // Assert
        result.Name.ShouldBeNull();
    }

    [Fact]
    public async Task SearchArtistByMBID_MusicBrainzFails_Unavailable()
    {
        // Arrange
        var client = CreateClient(new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        // Act
        var exception = await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistByMBID(Guid.NewGuid().ToString()));

        // Assert
        exception.Service.ShouldBe("MusicBrainz");
    }

    [Fact]
    public async Task SearchArtistsByName_MusicBrainzBusy_ArtistsOfLastFmThatHaveAnMbid()
    {
        // Arrange
        var lastFm = CreateLastFm();
        A.CallTo(() => lastFm.SearchArtistsByName(Artist, 5, 10)).Returns(new MusicArtistBase[]
        {
            new LastFmMusicArtist(new HqubArtist { Name = Artist, MBID = Mbid }),
            new LastFmMusicArtist(new HqubArtist { Name = "Metallica Tribute" }),
        });
        var client = CreateClient(Busy(), lastFm);

        // Act
        var result = (await client.SearchArtistsByName(Artist, limit: 5, offset: 10)).ToList();

        // Assert
        result.Select(x => (x.MBID, x.Name)).ShouldBe(new (string?, string?)[] { (Mbid, Artist) });
        result[0].IsMusicArtistExist(MusicServiceType.MusicBrainz).ShouldBeFalse();
        result[0].IsMusicArtistExist(MusicServiceType.LastFm).ShouldBeTrue();
        A.CallTo(() => lastFm.SearchArtistsByName(Artist, 5, 10)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchArtistsByName_MusicBrainzAnswers_LastFmIsNotAsked()
    {
        // Arrange
        var lastFm = CreateLastFm();
        var client = CreateClient(new FakeHttpMessageHandler(SearchAnswer), lastFm);

        // Act
        var result = (await client.SearchArtistsByName(Artist)).ToList();

        // Assert
        result[0].IsMusicArtistExist(MusicServiceType.MusicBrainz).ShouldBeTrue();
        A.CallTo(() => lastFm.SearchArtistsByName(A<string>._, A<int>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SearchArtistsByName_MusicBrainzAndLastFmBusy_Unavailable()
    {
        // Arrange
        var lastFm = CreateLastFm();
        A.CallTo(() => lastFm.SearchArtistsByName(Artist, 5, 0)).ThrowsAsync(new TimeoutException());
        var musicBrainz = Busy();
        var client = CreateClient(musicBrainz, lastFm);

        // Act
        var exception = await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistsByName(Artist));

        // Assert: MusicBrainz, Last.fm, MusicBrainz once more.
        exception.Service.ShouldBe("MusicBrainz");
        musicBrainz.Calls.ShouldBe(2);
        A.CallTo(() => lastFm.SearchArtistsByName(Artist, 5, 0)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchArtistsByName_MusicBrainzBusyAndLastFmFails_MusicBrainzAgainFindsTheArtists()
    {
        // Arrange
        var lastFm = CreateLastFm();
        A.CallTo(() => lastFm.SearchArtistsByName(Artist, 5, 0)).ThrowsAsync(new TimeoutException());
        var musicBrainz = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), SearchAnswer);
        var client = CreateClient(musicBrainz, lastFm);

        // Act
        var result = (await client.SearchArtistsByName(Artist)).ToList();

        // Assert
        result.Select(x => (x.MBID, x.Name)).ShouldBe(new (string?, string?)[] { (Mbid, Artist) });
        result[0].IsMusicArtistExist(MusicServiceType.MusicBrainz).ShouldBeTrue();
        musicBrainz.Calls.ShouldBe(2);
        A.CallTo(() => lastFm.SearchArtistsByName(Artist, 5, 0)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchArtistByMBID_MusicBrainzBusyAndLastFmFails_MusicBrainzAgainGivesTheFullCardAndCachesIt()
    {
        // Arrange: Last.fm fails both for the card and for the biography afterwards.
        var lastFm = CreateLastFm();
        A.CallTo(() => lastFm.GetAvailableSearch()).Returns(AvailableSearchType.MBID);
        A.CallTo(() => lastFm.GetArtistByMBID(Mbid)).ThrowsAsync(new Exception("LastFM did not answer"));
        var musicBrainz = new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), ArtistAnswer);
        var client = CreateClient(musicBrainz, lastFm);

        // Act
        var first = await client.SearchArtistByMBID(Mbid);
        var second = await client.SearchArtistByMBID(Mbid);

        // Assert
        foreach (var result in new[] { first, second })
        {
            (result.Name, result.MBID, result.Country).ShouldBe((Artist, Mbid, "US"));
            result.IsMusicArtistExist(MusicServiceType.MusicBrainz).ShouldBeTrue();
        }
        musicBrainz.Calls.ShouldBe(2);
        A.CallTo(() => lastFm.GetArtistByMBID(Mbid)).MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task SearchArtistsByName_MusicBrainzBusyAndLastFmHasNoArtistWithAnMbid_UnavailableNotNothingFound()
    {
        // Arrange
        var lastFm = CreateLastFm();
        A.CallTo(() => lastFm.SearchArtistsByName(Artist, 5, 0)).Returns(new MusicArtistBase[] { new LastFmMusicArtist(new HqubArtist { Name = "Metallica Tribute" }) });
        var client = CreateClient(Busy(), lastFm);

        // Act, Assert
        await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistsByName(Artist));
    }

    [Fact]
    public async Task SearchArtistByMBID_MusicBrainzBusy_CardOfLastFmNotCached()
    {
        // Arrange
        var lastFm = CreateLastFm();
        A.CallTo(() => lastFm.GetArtistByMBID(Mbid)).Returns(new LastFmMusicArtist(new HqubArtist { Name = Artist, MBID = Mbid, Url = "https://www.last.fm/music/Metallica" }));
        var musicBrainz = Busy();
        var client = CreateClient(musicBrainz, lastFm);

        // Act
        var first = await client.SearchArtistByMBID(Mbid);
        var second = await client.SearchArtistByMBID(Mbid);

        // Assert
        foreach (var result in new[] { first, second })
        {
            (result.Name, result.MBID, result.ExternalUrl).ShouldBe((Artist, Mbid, new Uri("https://www.last.fm/music/Metallica")));
            result.IsMusicArtistExist(MusicServiceType.MusicBrainz).ShouldBeFalse();
        }
        // Not cached: the second tap asks MusicBrainz again.
        musicBrainz.Calls.ShouldBe(2);
        A.CallTo(() => lastFm.GetArtistByMBID(Mbid)).MustHaveHappenedTwiceExactly();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SearchArtistByMBID_MusicBrainzBusyAndLastFmDoesNotHelp_Unavailable(bool lastFmFails)
    {
        // Arrange
        var lastFm = CreateLastFm();
        var call = A.CallTo(() => lastFm.GetArtistByMBID(Mbid));
        if (lastFmFails)
        {
            call.ThrowsAsync(new Exception("LastFM did not answer"));
        }
        else
        {
            call.Returns(new LastFmMusicArtist(new HqubArtist()));
        }
        var musicBrainz = Busy();
        var client = CreateClient(musicBrainz, lastFm);

        // Act
        var exception = await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistByMBID(Mbid));

        // Assert: MusicBrainz, Last.fm, MusicBrainz once more.
        exception.Service.ShouldBe("MusicBrainz");
        musicBrainz.Calls.ShouldBe(2);
        A.CallTo(() => lastFm.GetArtistByMBID(Mbid)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchArtistByMBID_MusicBrainzHasNoSuchArtist_LastFmIsNotAsked()
    {
        // Arrange
        var lastFm = CreateLastFm();
        var client = CreateClient(new FakeHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NotFound)), lastFm);

        // Act
        var result = await client.SearchArtistByMBID(Mbid);

        // Assert
        result.Name.ShouldBeNull();
        A.CallTo(() => lastFm.GetArtistByMBID(A<string>._)).MustNotHaveHappened();
    }

    private static FakeHttpMessageHandler Busy() => new(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

    private static IMusicServiceClient CreateLastFm()
    {
        var lastFm = A.Fake<IMusicServiceClient>();
        A.CallTo(() => lastFm.MusicServiceType).Returns(MusicServiceType.LastFm);
        return lastFm;
    }

    private void AssertLogged(LogLevel level, int times, string messageStart = "")
    {
        A.CallTo(_logger)
            .Where(call => call.Method.Name == nameof(ILogger.Log)
                && call.GetArgument<LogLevel>(0) == level
                && Message(call.GetArgument<object>(2)).StartsWith(messageStart, StringComparison.Ordinal))
            .MustHaveHappened(times, Times.Exactly);
    }

    private static string Message(object? state) => state?.ToString() ?? string.Empty;

    private MusicSearcherClient CreateClient(params IMusicServiceClient[] services)
    {
        return new MusicSearcherClient(_lyricsClient, _setlistClient, services, new MusicBrainzClient(), _logger);
    }

    private MusicSearcherClient CreateClient(HttpMessageHandler musicBrainz, params IMusicServiceClient[] services)
    {
        var httpClient = new HttpClient(musicBrainz) { BaseAddress = new Uri(MusicBrainzClient.ServiceBaseAddress) };
        return new MusicSearcherClient(_lyricsClient, _setlistClient, services, new MusicBrainzClient(httpClient), A.Fake<ILogger<MusicSearcherClient>>());
    }

    private static string UniqueName() => $"test-{Guid.NewGuid():N}";

    /// <summary>
    /// A MusicBrainz artist search with one artist, in the JSON the client asks for.
    /// </summary>
    private static HttpResponseMessage SearchAnswer()
    {
        var json = $$"""{"created":"2026-11-21T00:00:00.000Z","count":1,"offset":0,"artists":[{"id":"{{Mbid}}","type":"Group","score":100,"name":"{{Artist}}","sort-name":"{{Artist}}","country":"US"}]}""";
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
    }

    /// <summary>
    /// A MusicBrainz artist by MBID, in the JSON the client asks for.
    /// </summary>
    private static HttpResponseMessage ArtistAnswer()
    {
        var json = $$"""{"id":"{{Mbid}}","type":"Group","name":"{{Artist}}","sort-name":"{{Artist}}","country":"US"}""";
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
    }

    private static IMusicServiceClient CreateService(MusicTrackBase track)
    {
        var service = A.Fake<IMusicServiceClient>();
        A.CallTo(() => service.SearchTrack(A<string>._, A<string>._)).Returns(track);
        return service;
    }

    private static MusicTrackBase CreateTrack(MusicServiceType type, string name, string? downloadLink = null, string? externalLink = null)
    {
        var track = A.Fake<MusicTrackBase>();
        A.CallTo(() => track.MusicServiceType).Returns(type);
        A.CallTo(() => track.TrackName).Returns(name);
        A.CallTo(() => track.DownloadLink).Returns(downloadLink);
        A.CallTo(() => track.TrackExternalLink).Returns(externalLink);
        return track;
    }
}
