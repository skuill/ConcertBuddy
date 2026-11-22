using FakeItEasy;
using Hqub.Lastfm;
using Hqub.Lastfm.Entities;
using Microsoft.Extensions.Time.Testing;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.LastFm;
using MusicSearcher.MusicService;
using MusicSearcher.MusicService.LastFm;
using Shouldly;

namespace MusicSearcher.Tests.MusicService.LastFm;

public class LastFmServiceClientTest
{
    private const string Mbid = "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab";
    private const string ArtistName = "Metallica";
    private const int ExpectedLimit = 10;
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(5);

    private static readonly Track[] Tracks =
    {
        new Track { Name = "Enter Sandman", Url = "https://www.last.fm/music/Metallica/_/Enter+Sandman", Artist = new Artist { Name = ArtistName } },
        new Track { Name = "One", Url = "https://www.last.fm/music/Metallica/_/One", Artist = new Artist { Name = ArtistName } },
    };

    private readonly ILastFmApi _api = A.Fake<ILastFmApi>();

    [Fact]
    public async Task SearchTopTracks_MbidIsKnown_ReturnsTracksByMbid()
    {
        // Arrange
        A.CallTo(() => _api.GetTopTracksByMbidAsync(Mbid, ExpectedLimit)).Returns(Tracks);
        var client = new LastFmServiceClient(_api);

        // Act
        var result = await client.SearchTopTracks(CreateArtist(Mbid, ArtistName));

        // Assert
        AssertTracks(result);
        A.CallTo(() => _api.GetTopTracksByMbidAsync(Mbid, ExpectedLimit)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _api.GetTopTracksAsync(A<string>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SearchTopTracks_MbidIsUnknownToLastFm_ReturnsTracksByName()
    {
        // Arrange
        A.CallTo(() => _api.GetTopTracksByMbidAsync(Mbid, ExpectedLimit))
            .ThrowsAsync(new ServiceException("artist.getTopTracks", 6, "The artist you supplied could not be found"));
        A.CallTo(() => _api.GetTopTracksAsync(ArtistName, ExpectedLimit)).Returns(Tracks);
        var client = new LastFmServiceClient(_api);

        // Act
        var result = await client.SearchTopTracks(CreateArtist(Mbid, ArtistName));

        // Assert
        AssertTracks(result);
        A.CallTo(() => _api.GetTopTracksAsync(ArtistName, ExpectedLimit)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchTopTracks_NoTracksByMbid_ReturnsTracksByName()
    {
        // Arrange
        A.CallTo(() => _api.GetTopTracksByMbidAsync(Mbid, ExpectedLimit)).Returns(new List<Track>());
        A.CallTo(() => _api.GetTopTracksAsync(ArtistName, ExpectedLimit)).Returns(Tracks);
        var client = new LastFmServiceClient(_api);

        // Act
        var result = await client.SearchTopTracks(CreateArtist(Mbid, ArtistName));

        // Assert
        AssertTracks(result);
        A.CallTo(() => _api.GetTopTracksAsync(ArtistName, ExpectedLimit)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchTopTracks_NoMbid_DoesNotCallLastFmByMbid()
    {
        // Arrange
        A.CallTo(() => _api.GetTopTracksAsync(ArtistName, ExpectedLimit)).Returns(Tracks);
        var client = new LastFmServiceClient(_api);

        // Act
        var result = await client.SearchTopTracks(CreateArtist(null, ArtistName));

        // Assert
        AssertTracks(result);
        A.CallTo(() => _api.GetTopTracksByMbidAsync(A<string>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SearchTopTracks_NoMbidAndNoName_ReturnsEmptyWithoutCalls()
    {
        // Arrange
        var client = new LastFmServiceClient(_api);

        // Act
        var result = await client.SearchTopTracks(CreateArtist(null, null));

        // Assert
        result.ShouldBeEmpty();
        A.CallTo(_api).MustNotHaveHappened();
    }

    [Fact]
    public async Task SearchTopTracks_ErrorByName_IsNotSwallowed()
    {
        // Arrange
        A.CallTo(() => _api.GetTopTracksAsync(ArtistName, ExpectedLimit))
            .ThrowsAsync(new ServiceException("artist.getTopTracks", 29, "Rate limit exceeded"));
        var client = new LastFmServiceClient(_api);

        // Act, Assert
        await Should.ThrowAsync<ServiceException>(() => client.SearchTopTracks(CreateArtist(null, ArtistName)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(5, 2)]
    [InlineData(10, 3)]
    public async Task SearchArtistsByName_Offset_AsksThePageOfLastFm(int offset, int expectedPage)
    {
        // Arrange
        var found = new[]
        {
            new Artist { Name = ArtistName, MBID = Mbid, Url = "https://www.last.fm/music/Metallica", Statistics = new Statistics { Listeners = 3_100_000 } },
            new Artist { Name = "Metallica Tribute", Url = "https://www.last.fm/music/Metallica+Tribute" },
        };
        A.CallTo(() => _api.SearchArtistsAsync(ArtistName, expectedPage, 5)).Returns(found);
        var client = new LastFmServiceClient(_api);

        // Act
        var result = await client.SearchArtistsByName(ArtistName, limit: 5, offset: offset);

        // Assert
        result.Select(x => (x.MusicServiceType, x.Name, x.MBID, x.ExternalUrl, x.Listeners)).ShouldBe(new (MusicServiceType, string?, string?, Uri?, long?)[]
        {
            (MusicServiceType.LastFm, ArtistName, Mbid, new Uri("https://www.last.fm/music/Metallica"), 3_100_000),
            (MusicServiceType.LastFm, "Metallica Tribute", null, new Uri("https://www.last.fm/music/Metallica+Tribute"), null),
        });
        A.CallTo(() => _api.SearchArtistsAsync(ArtistName, expectedPage, 5)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchArtistsByName_LastFmDoesNotAnswer_GivesUpAfterTheTimeout()
    {
        // Arrange: Hqub.Lastfm takes no timeout, so the client stops waiting itself.
        var time = new FakeTimeProvider();
        A.CallTo(() => _api.SearchArtistsAsync(ArtistName, 1, 5)).Returns(new TaskCompletionSource<IReadOnlyList<Artist>>().Task);
        var client = new LastFmServiceClient(_api, time);
        var search = client.SearchArtistsByName(ArtistName, limit: 5, offset: 0);

        // Act
        time.Advance(LastFmServiceClient.DefaultTimeout - TimeSpan.FromMilliseconds(1));
        var doneBefore = search.IsCompleted;
        time.Advance(TimeSpan.FromMilliseconds(1));

        // Assert
        doneBefore.ShouldBeFalse();
        // A safety wait, so a missing limit fails the test instead of hanging it.
        await Task.WhenAny(search, Task.Delay(SafetyTimeout, TestContext.Current.CancellationToken));
        search.IsCompleted.ShouldBeTrue();
        await Should.ThrowAsync<TimeoutException>(() => search);
        LastFmServiceClient.DefaultTimeout.ShouldBe(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public async Task GetArtistByMBID_LastFmDoesNotAnswer_FailsAfterTheTimeout()
    {
        // Arrange
        var time = new FakeTimeProvider();
        A.CallTo(() => _api.GetArtistInfoByMbidAsync(Mbid)).Returns(new TaskCompletionSource<Artist>().Task);
        var client = new LastFmServiceClient(_api, time);
        var artist = client.GetArtistByMBID(Mbid);

        // Act
        time.Advance(LastFmServiceClient.DefaultTimeout);

        // Assert
        await Task.WhenAny(artist, Task.Delay(SafetyTimeout, TestContext.Current.CancellationToken));
        artist.IsCompleted.ShouldBeTrue();
        var exception = await Should.ThrowAsync<Exception>(() => artist);
        exception.InnerException.ShouldBeOfType<TimeoutException>();
        exception.Message.ShouldBe($"LastFM did not answer for MBID [{Mbid}] in 4 s.");
    }

    [Fact]
    public void LastFmMusicTrack_NoArtist_HasNoArtistsNames()
    {
        // Act
        var result = new LastFmMusicTrack(new Track { Name = "One" });

        // Assert
        result.TrackName.ShouldBe("One");
        result.ArtistsNames.ShouldBeNull();
    }

    private static MusicArtistBase CreateArtist(string? mbid, string? name)
    {
        return new LastFmMusicArtist(new Artist { MBID = mbid, Name = name });
    }

    private static void AssertTracks(List<MusicTrackBase> result)
    {
        result.Count.ShouldBe(Tracks.Length);
        for (int i = 0; i < Tracks.Length; i++)
        {
            result[i].MusicServiceType.ShouldBe(MusicServiceType.LastFm);
            result[i].TrackName.ShouldBe(Tracks[i].Name);
            result[i].TrackExternalLink.ShouldBe(Tracks[i].Url);
            result[i].ArtistsNames.ShouldBe(new[] { ArtistName });
            result[i].DownloadLink.ShouldBeNull();
            result[i].AlbumName.ShouldBeNull();
            result[i].AlbumExternalLink.ShouldBeNull();
            result[i].ArtistsExternalLinks.ShouldBeNull();
            result[i].Duration.ShouldBeNull();
        }
    }
}
