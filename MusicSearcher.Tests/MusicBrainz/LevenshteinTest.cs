using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSearcher.MusicBrainz;

namespace MusicSearcher.Tests.MusicBrainz
{
    [TestClass]
    public class LevenshteinTest
    {
        [TestMethod]
        public void Similarity_SameArtist_AreEqual()
        {
            // Arrange
            string artist1 = "Parkway Drive";
            string artist2 = "Parkway Drive";

            // Act
            var score = Levenshtein.Similarity(artist1, artist2);

            // Assert
            Assert.AreEqual(100, score);
        }

        [TestMethod]
        public void Similarity_DifferentArtist_AreNotEqual()
        {
            // Arrange
            string artist1 = "Parkway Drive";
            string artist2 = "RHCP";

            // Act
            var score = Levenshtein.Similarity(artist1, artist2);

            // Assert
            Assert.AreEqual(0, score);
        }

        [TestMethod]
        public void Similarity_AbbreviatedArtist_ZeroScore()
        {
            // Arrange
            string artist1 = "RHCP";
            string artist2 = "Red Hot Chili Peppers";

            // Act
            var score = Levenshtein.Similarity(artist1, artist2);

            // Assert
            Assert.AreEqual(0, score);
        }

        [TestMethod]
        public void Similarity_LowercaseArtist_AreEqual()
        {
            // Arrange
            string artist1 = "parkway drive";
            string artist2 = "Parkway Drive";

            // Act
            var score = Levenshtein.Similarity(artist1, artist2);

            // Assert
            Assert.AreEqual(100, score);
        }
    }
}
