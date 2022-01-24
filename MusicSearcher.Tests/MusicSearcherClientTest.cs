using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MusicSearcher.Abstract;
using System.Linq;
using System.Threading.Tasks;

namespace MusicSearcher.Tests
{
    [TestClass]
    public class MusicSearcherClientTest
    {
        [TestMethod]
        public async Task SearchArtistByName_EmptyArtist_IsNull()
        {
            // Arrange
            string artist = "";
            var client = InitClient();

            // Act
            var result = await client.SearchArtistByName(artist);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task SearchArtistByName_DefaultExample_AreEqual()
        {
            // Arrange
            string artist = "Parkway Drive";
            var client = InitClient();
                            
            // Act
            var result = await client.SearchArtistByName(artist);

            // Assert
            Assert.IsTrue(string.Equals(artist, result.Name));
        }

        [TestMethod]
        public async Task SearchArtistByName_LowercaseArtist_AreEqual()
        {
            // Arrange
            string artist = "parkway drive";
            string expected = "Parkway Drive";
            var client = InitClient();

            // Act
            var result = await client.SearchArtistByName(artist);

            // Assert
            Assert.IsTrue(string.Equals(expected, result.Name));
        }

        [TestMethod]
        [Ignore("abbreviations don't work correctly")]
        public async Task SearchArtistByName_AbbreviatedArtist_AreEqual()
        {
            // Arrange
            string artist = "RHCP";
            string expected = "Red Hot Chili Peppers";
            var client = InitClient();

            // Act
            var result = await client.SearchArtistByName(artist);

            // Assert
            Assert.IsTrue(string.Equals(expected, result.Name));
        }

        [TestMethod]
        public async Task SearchArtistByMBID_MalformedMbid_IsNull()
        {
            // Arrange
            string mbid = "gafgxvbasdf23423";
            var client = InitClient();

            // Act
            var result = await client.SearchArtistByMBID(mbid);

            // Assert
            Assert.IsNull(result.MusicBrainzArtist);
            Assert.IsNull(result.LastFmArtist);
            Assert.IsTrue(string.IsNullOrEmpty(result.Name));
            Assert.IsTrue(string.IsNullOrEmpty(result.Biography));
            Assert.IsTrue(string.IsNullOrEmpty(result.MBID));
        }

        [TestMethod]
        public async Task SearchArtistByMBID_DefaultExample_AreEqual()
        {
            // Arrange
            string artist = "The Beatles";
            string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";
            var client = InitClient();

            // Act
            var result = await client.SearchArtistByMBID(mbid);

            // Assert
            Assert.IsTrue(string.Equals(artist, result.Name));
        }

        [TestMethod]
        public async Task SearchArtistByMBID_WithLastFmClient_AreEqual()
        {
            // Arrange
            string artist = "The Beatles";
            string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";
            var client = InitClient();
            client.WithLastFmClient(Configuration.LastFmApiKey, Configuration.LastFmApiSecret);

            // Act
            var result = await client.SearchArtistByMBID(mbid);

            // Assert
            Assert.IsNotNull(result.LastFmArtist);
            Assert.IsTrue(string.Equals(artist, result.Name));
            Assert.IsTrue(string.Equals(artist, result.LastFmArtist.Name));
        }

        [TestMethod]
        public async Task SearchArtistByMBID_WithSpotifyClient_AreEqual()
        {
            // Arrange
            string artist = "The Beatles";
            string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";
            var client = InitClient();
            client.WithSpotifyClient(Configuration.SpotifyClientID, Configuration.SpotifyClientSecret);

            // Act
            var result = await client.SearchArtistByMBID(mbid);

            // Assert
            Assert.IsNotNull(result.SpotifyArtist);
            Assert.IsTrue(string.Equals(artist, result.Name));
            Assert.IsTrue(string.Equals(artist, result.SpotifyArtist.Name));
            Assert.IsNotNull(result.ImageUri);
        }

        [TestMethod]
        public async Task SearchSpotifyTrack_DefaultExample_AreEqual()
        {
            // Arrange
            string artist = "Parkway Drive";
            string trackSearch = "wishing wells";
            string trackExpected = "Wishing Wells";
            var client = InitClient();
            client.WithSpotifyClient(Configuration.SpotifyClientID, Configuration.SpotifyClientSecret);

            // Act
            var result = await client.SearchTrack(artist, trackSearch);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(trackExpected, result.Name));
            Assert.IsNotNull(result.SpotifyTrack.Artists);
            Assert.IsTrue(result.SpotifyTrack.Artists.Any(x => string.Equals(x.Name, artist)));
        }

        [TestMethod]
        public async Task SearchSongByName_DefaultExample_AreEqual()
        {
            // Arrange
            string artistMBID = "a436dd02-0549-4c91-b608-df451217fdeb";
            string songName = "Wishing Wells";

            var expectedArtistName = "ParkwayDrive";
            var expectedRecordingMBID = "a99d7f83-75b1-46f6-8b71-cd230b7c3060";

            var client = InitClient();

            // Act
            var result = await client.SearchSongByName(artistMBID, songName);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(expectedRecordingMBID, result.Id));
        }

        private IMusicSearcherClient InitClient()
        {
            var loggerMock = Mock.Of<ILogger<MusicSearcherClient>>();
            return new MusicSearcherClient(loggerMock);
        }
    }
}