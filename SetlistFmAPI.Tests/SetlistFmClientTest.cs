using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            ISetlistFmClient client = new SetlistFmClient(null, new HttpSetlistWebClient(null));
            client.WithApiKey(AppSettings.SetlistFmApiKey);
            var result = await client.SearchArtist("b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d");

            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.Name, "The Beatles"));
            Assert.IsTrue(string.Equals(result.SortName, "Beatles, The"));
            Assert.IsTrue(string.Equals(result.Url, "https://www.setlist.fm/setlists/the-beatles-23d6a88b.html"));
        }

        [TestMethod]
        public async Task SearchArtists_ByName_NotNull()
        {
            ISetlistFmClient client = new SetlistFmClient(null, new HttpSetlistWebClient(null));
            client.WithApiKey(AppSettings.SetlistFmApiKey);
            var result = await client.SearchArtists("Parkway Drive");

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Items);
            Assert.IsTrue(result.Items.Count == 3);
        }

        [TestMethod]
        public async Task SearchArtistSetlists_ByMBID_NotNull()
        {
            string mbid = "a436dd02-0549-4c91-b608-df451217fdeb";

            ISetlistFmClient client = new SetlistFmClient(null, new HttpSetlistWebClient(null));
            client.WithApiKey(AppSettings.SetlistFmApiKey);
            var result = await client.SearchArtistSetlists(mbid);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Total > 0);
            Assert.IsTrue(string.Equals(result.ApiType, "setlists"));
            Assert.IsNotNull(result.Items);
            Assert.IsTrue(result.Items.Count > 0);
        }

        [TestMethod]
        public async Task SearchSetlist_BySetlistId_NotNull()
        {
            string setlistId = "539a63ed";

            ISetlistFmClient client = new SetlistFmClient(null, new HttpSetlistWebClient(null));
            client.WithApiKey(AppSettings.SetlistFmApiKey);
            var result = await client.SearchSetlist(setlistId);

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
    }
}