using FakeItEasy;
using Hqub.Lastfm;
using Hqub.Lastfm.Entities;
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
