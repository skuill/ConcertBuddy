using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SetlistFmAPI.Tests
{
    [TestClass]
    public class SetlistFmClientTest
    {
        private string _apiKey = "";

        [TestInitialize]
        public void TestInitialize()
        {
            // sativkv@gmail.com API
            this._apiKey = "YswjYvgRtNyqt6zHfVQavkh8uyf_iP6ZWQjv";
        }

        [TestMethod]
        public async Task SearchArtist_ByMBID_NotNull()
        {
            ISetlistFmClient client = new SetlistFmClient(_apiKey);
            var result = await client.SearchArtist("b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d");

            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.Name, "The Beatles"));
            Assert.IsTrue(string.Equals(result.SortName, "Beatles, The"));
            Assert.IsTrue(string.Equals(result.Url, "https://www.setlist.fm/setlists/the-beatles-23d6a88b.html"));
        }

        [TestMethod]
        public async Task SearchArtists_ByName_NotNull()
        {
            ISetlistFmClient client = new SetlistFmClient(_apiKey);
            var result = await client.SearchArtists("Parkway Drive");

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Items);
            Assert.IsTrue(result.Items.Count == 3);
        }
    }
}