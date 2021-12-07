using Microsoft.VisualStudio.TestTools.UnitTesting;
using SetlistFmAPI.Models;
using System;

namespace SetlistFmAPI.Tests
{
    [TestClass]
    public class SetlistFmUrlsTest
    {
        [TestMethod]
        public void RelativeUri_WithStartSlash_IsTrimmed()
        {
            FormattableString path = $"/artist/b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";

            Uri result = SetlistFmUrls.RelativeUri(path);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAbsoluteUri);
            Assert.IsTrue(string.Equals(result.ToString(), "artist/b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d"));
        }

        [TestMethod]
        public void Artist_SimpleMbid_NotNull()
        {
            string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";

            Uri result = SetlistFmUrls.Artist(mbid);

            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"artist/{mbid}"));
        }

        [TestMethod]
        public void Artists_ByName_NotNull()
        {
            string bandName = "Parkway Drive";
            Artist artist = new Artist(bandName); 

            Uri result = SetlistFmUrls.Artists(artist);

            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"search/artists?artistName={bandName}&sort=relevance&p=1"));
        }

        [TestMethod]
        public void ArtistSetlists_SimpleMbid_NotNull()
        {
            string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";

            Uri result = SetlistFmUrls.ArtistSetlists(mbid, 5);

            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"artist/{mbid}/setlists?p=5"));
        }

        [TestMethod]
        public void Setlist_SimpleID_NotNull()
        {
            string setlistId = "539a63ed";

            Uri result = SetlistFmUrls.Setlist(setlistId);

            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"setlist/{setlistId}"));
        }
    }
}
