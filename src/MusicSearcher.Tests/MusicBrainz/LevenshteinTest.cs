using MusicSearcher.MusicBrainz;
using Shouldly;

namespace MusicSearcher.Tests.MusicBrainz;

public class LevenshteinTest
{
    [Theory]
    [InlineData("Parkway Drive", "Parkway Drive", 100)]
    [InlineData("parkway drive", "Parkway Drive", 100)]
    [InlineData("Parkway Drive", "RHCP", 0)]
    [InlineData("RHCP", "Red Hot Chili Peppers", 0)]
    public void Similarity_TwoArtists_ReturnsScore(string artist1, string artist2, int expected)
    {
        // Act
        var score = Levenshtein.Similarity(artist1, artist2);

        // Assert
        score.ShouldBe(expected);
    }
}
