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
using Shouldly;

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

    private MusicSearcherClient CreateClient(params IMusicServiceClient[] services)
    {
        return new MusicSearcherClient(_lyricsClient, _setlistClient, services, new MusicBrainzClient(), A.Fake<ILogger<MusicSearcherClient>>());
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
