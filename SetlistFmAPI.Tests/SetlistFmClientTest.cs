using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SetlistFmAPI.Http;
using System.Threading.Tasks;

namespace SetlistFmAPI.Tests
{
    [TestClass]
    public class SetlistFmClientTest
    {
        [TestMethod]
        public async Task SearchArtist_ByMBID_NotNull()
        {
            // Arrange
            var client = InitClient();

            // Act
            var result = await client.SearchArtist("b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.Name, "The Beatles"));
            Assert.IsTrue(string.Equals(result.SortName, "Beatles, The"));
            Assert.IsTrue(string.Equals(result.Url, "https://www.setlist.fm/setlists/the-beatles-23d6a88b.html"));
        }

        [TestMethod]
        public async Task SearchArtists_ByName_NotNull()
        {
            // Arrange
            var client = InitClient();

            // Act
            var result = await client.SearchArtists("Parkway Drive");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Items);
            Assert.IsTrue(result.Items.Count == 3);
        }

        [TestMethod]
        public async Task SearchArtistSetlists_ByMBID_NotNull()
        {
            // Arrange
            string mbid = "a436dd02-0549-4c91-b608-df451217fdeb";
            var client = InitClient();

            // Act
            var result = await client.SearchArtistSetlists(mbid);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Total > 0);
            Assert.IsTrue(string.Equals(result.ApiType, "setlists"));
            Assert.IsNotNull(result.Items);
            Assert.IsTrue(result.Items.Count > 0);
        }

        [TestMethod]
        public async Task SearchSetlist_BySetlistId_NotNull()
        {
            // Arrange
            string setlistId = "539a63ed";
            var client = InitClient();

            // Act
            var result = await client.SearchSetlist(setlistId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Artist);
            Assert.IsTrue(string.Equals(result.Artist.Name, "Parkway Drive"));
            Assert.IsNotNull(result.Sets);
            Assert.IsNotNull(result.Sets.Items);
            Assert.IsTrue(result.Sets.Items.Count == 1);
            var bandSet = result.Sets.Items[0];
            Assert.IsNotNull(bandSet.Songs);
            Assert.IsTrue(bandSet.Songs.Count == 14);
            Assert.IsTrue(string.Equals(bandSet.Songs[0].Name, "Wishing Wells"));
            Assert.IsNull(result.Tour);
            Assert.IsNotNull(result.Venue);
            Assert.IsTrue(string.Equals(result.Venue.Name, "Brisbane Showgrounds"));
            Assert.IsNotNull(result.Venue.City);
            Assert.IsTrue(string.Equals(result.EventDate, "08-12-2019"));
            Assert.IsTrue(string.Equals(result.Url, "https://www.setlist.fm/setlist/parkway-drive/2019/brisbane-showgrounds-brisbane-australia-539a63ed.html"));
        }

        private ISetlistFmClient InitClient()
        {
            var loggerWebMock = Mock.Of<ILogger<SetlistHttpWebClient>>();
            var webClient = new SetlistHttpWebClient(loggerWebMock);

            var loggerClientMock = Mock.Of<ILogger<SetlistFmClient>>();
            ISetlistFmClient client = new SetlistFmClient(loggerClientMock, webClient);
            client.WithApiKey(Configuration.SetlistFmApiKey);
            return client;
        }
    }
}