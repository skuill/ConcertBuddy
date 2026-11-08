using FakeItEasy;
using Hqub.Lastfm;
using Hqub.Lastfm.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.LastFm;
using MusicSearcher.MusicService;
using MusicSearcher.MusicService.LastFm;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicSearcher.Tests.MusicService.LastFm
{
    [TestClass]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public async Task SearchTopTracks_NoMbidAndNoName_ReturnsEmptyWithoutCalls()
        {
            // Arrange
            var client = new LastFmServiceClient(_api);

            // Act
            var result = await client.SearchTopTracks(CreateArtist(null, null));

            // Assert
            Assert.AreEqual(0, result.Count);
            A.CallTo(_api).MustNotHaveHappened();
        }

        [TestMethod]
        public async Task SearchTopTracks_OtherErrorByName_IsNotSwallowed()
        {
            // Arrange
            A.CallTo(() => _api.GetTopTracksAsync(ArtistName, ExpectedLimit))
                .ThrowsAsync(new ServiceException("artist.getTopTracks", 29, "Rate limit exceeded"));
            var client = new LastFmServiceClient(_api);

            // Act, Assert
            await Assert.ThrowsExceptionAsync<ServiceException>(() => client.SearchTopTracks(CreateArtist(null, ArtistName)));
        }

        [TestMethod]
        public void LastFmMusicTrack_NoArtist_HasNoArtistsNames()
        {
            // Act
            var result = new LastFmMusicTrack(new Track { Name = "One" });

            // Assert
            Assert.AreEqual("One", result.TrackName);
            Assert.IsNull(result.ArtistsNames);
        }

        private static MusicArtistBase CreateArtist(string? mbid, string? name)
        {
            return new LastFmMusicArtist(new Artist { MBID = mbid, Name = name });
        }

        private static void AssertTracks(List<MusicTrackBase> result)
        {
            Assert.AreEqual(Tracks.Length, result.Count);
            for (int i = 0; i < Tracks.Length; i++)
            {
                Assert.AreEqual(MusicServiceType.LastFm, result[i].MusicServiceType);
                Assert.AreEqual(Tracks[i].Name, result[i].TrackName);
                Assert.AreEqual(Tracks[i].Url, result[i].TrackExternalLink);
                CollectionAssert.AreEqual(new[] { ArtistName }, result[i].ArtistsNames!.ToArray());
                Assert.IsNull(result[i].DownloadLink);
                Assert.IsNull(result[i].AlbumName);
                Assert.IsNull(result[i].AlbumExternalLink);
                Assert.IsNull(result[i].ArtistsExternalLinks);
                Assert.IsNull(result[i].Duration);
            }
        }
    }
}
