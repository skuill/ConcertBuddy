using FakeItEasy;
using Hqub.MusicBrainz;
using LyricsScraperNET;
using LyricsScraperNET.Models.Requests;
using LyricsScraperNET.Models.Responses;
using Microsoft.Extensions.Logging;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
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
        // Arrange: a unique name, so the file cache of MusicBrainz responses has nothing for it.
        var handler = new FakeHttpMessageHandler(() => new HttpResponseMessage(status));
        var client = CreateClient(handler);

        // Act
        var exception = await Should.ThrowAsync<MusicServiceUnavailableException>(() => client.SearchArtistsByName(UniqueName()));

        // Assert
        exception.Service.ShouldBe("MusicBrainz");
        handler.Calls.ShouldBe(1);
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

    private MusicSearcherClient CreateClient(params IMusicServiceClient[] services)
    {
        return new MusicSearcherClient(_lyricsClient, _setlistClient, services, new MusicBrainzClient(), A.Fake<ILogger<MusicSearcherClient>>());
    }

    private MusicSearcherClient CreateClient(HttpMessageHandler musicBrainz)
    {
        var httpClient = new HttpClient(musicBrainz) { BaseAddress = new Uri(MusicBrainzClient.ServiceBaseAddress) };
        return new MusicSearcherClient(_lyricsClient, _setlistClient, Array.Empty<IMusicServiceClient>(), new MusicBrainzClient(httpClient), A.Fake<ILogger<MusicSearcherClient>>());
    }

    private static string UniqueName() => $"test-{Guid.NewGuid():N}";

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
