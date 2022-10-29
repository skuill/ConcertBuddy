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
            // Arrange
            FormattableString path = $"/artist/b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";

            // Act
            Uri result = SetlistFmUrls.RelativeUri(path);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsAbsoluteUri);
            Assert.IsTrue(string.Equals(result.ToString(), "artist/b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d"));
        }

        [TestMethod]
        public void Artist_SimpleMbid_NotNull()
        {
            // Arrange
            string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";

            // Act
            Uri result = SetlistFmUrls.Artist(mbid);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"artist/{mbid}"));
        }

        [TestMethod]
        public void Artists_ByName_NotNull()
        {
            // Arrange
            string bandName = "Parkway Drive";
            Artist artist = new Artist(bandName);

            // Act
            Uri result = SetlistFmUrls.Artists(artist);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"search/artists?artistName={bandName}&sort=relevance&p=1"));
        }

        [TestMethod]
        public void ArtistSetlists_SimpleMbid_NotNull()
        {
            // Arrange
            string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";

            // Act
            Uri result = SetlistFmUrls.ArtistSetlists(mbid, 5);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"artist/{mbid}/setlists?p=5"));
        }

        [TestMethod]
        public void Setlist_SimpleID_NotNull()
        {
            // Arrange
            string setlistId = "539a63ed";

            // Act
            Uri result = SetlistFmUrls.Setlist(setlistId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(string.Equals(result.ToString(), $"setlist/{setlistId}"));
        }
    }
}
