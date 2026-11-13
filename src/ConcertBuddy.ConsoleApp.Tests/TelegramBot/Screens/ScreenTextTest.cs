using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class ScreenTextTest
{
    [Theory]
    [InlineData("US", "🇺🇸")]
    [InlineData("gb", "🇬🇧")]
    [InlineData(null, "🎤")]
    [InlineData("", "🎤")]
    [InlineData("USA", "🎤")]
    [InlineData("X1", "🎤")]
    public void Flag_CountryCode_FlagOrMicrophone(string? country, string expected)
    {
        ScreenText.Flag(country).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Short text", 20, "Short text")]
    [InlineData("Exactly ten", 11, "Exactly ten")]
    [InlineData("One two three four", 12, "One two…")]
    [InlineData("Supercalifragilistic", 10, "Supercalif…")]
    [InlineData("A verylongwordwithoutspaces", 12, "A verylongwo…")]
    public void Shorten_Text_CutAtAWordBoundary(string text, int maxLength, string expected)
    {
        ScreenText.Shorten(text, maxLength).ShouldBe(expected);
    }

    [Fact]
    public void Describe_AllParts_JoinedWithDots()
    {
        ScreenText.Describe(TestArtist.Metallica()).ShouldBe("Group · United States · 1981 -");
    }

    [Fact]
    public void Describe_EmptyParts_Skipped()
    {
        // Arrange: MusicBrainz gives " - " when it knows no years.
        var artist = new TestArtist { NameValue = "Somebody", TypeValue = "", AreaValue = "Finland", ActiveYearsValue = " - " };

        // Act & Assert
        ScreenText.Describe(artist).ShouldBe("Finland");
    }

    [Fact]
    public void Biography_LastFmSummary_PlainTextWithoutTheLink()
    {
        // Arrange
        var summary = "Metallica is a <b>heavy metal</b> band &amp; more. <a href=\"https://www.last.fm/music/Metallica\">Read more on Last.fm</a>";

        // Act & Assert
        ScreenText.Biography(summary).ShouldBe("Metallica is a heavy metal band & more.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Biography_Nothing_Empty(string? biography)
    {
        ScreenText.Biography(biography).ShouldBeEmpty();
    }

    [Fact]
    public void Html_SpecialCharacters_Encoded()
    {
        ScreenText.Html("<b>\"AC&DC\"</b> · Björk").ShouldBe("&lt;b&gt;&quot;AC&amp;DC&quot;&lt;/b&gt; · Björk");
    }
}
