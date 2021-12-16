using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MusicSearcher.Abstract;
using MusicSearcher.MusicBrainz;
using System.Threading.Tasks;

namespace MusicSearcher.Tests.MusicBrainz
{
    [TestClass]
    public class MusicBrainzSearcherClientTest
    {
        [TestMethod]
        public async Task SearchArtist_DefaultExample_AreEqual()
        {
            // Arrange
            string artist = "Parkway Drive";
            var client = InitClient();
                            
            // Act
            var result = await client.SearchArtist(artist);

            // Assert
            Assert.IsTrue(string.Equals(artist, result));
        }

        [TestMethod]
        public async Task SearchArtist_LowercaseArtist_AreEqual()
        {
            // Arrange
            string artist = "parkway drive";
            string expected = "Parkway Drive";
            var client = InitClient();

            // Act
            var result = await client.SearchArtist(artist);

            // Assert
            Assert.IsTrue(string.Equals(expected, result));
        }

        [TestMethod]
        [Ignore("abbreviations don't work correctly")]
        public async Task SearchArtist_AbbreviatedArtist_AreEqual()
        {
            // Arrange
            string artist = "RHCP";
            string expected = "Red Hot Chili Peppers";
            var client = InitClient();

            // Act
            var result = await client.SearchArtist(artist);

            // Assert
            Assert.IsTrue(string.Equals(expected, result));
        }

        private IMusicSearcherClient InitClient()
        {
            var loggerMock = Mock.Of<ILogger<MusicBrainzSearcherClient>>();
            return new MusicBrainzSearcherClient(loggerMock);
        }
    }
}