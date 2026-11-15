using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class InfoScreensTest
{
    private readonly InfoScreens _screens = new(new CallbackCodec());

    [Fact]
    public void Start_FirstName_GreetingExamplesHelpAndAbout()
    {
        ScreenDump.Of(_screens.Start("<Vi>")).ShouldBe("""
            👋 Hi, &lt;Vi&gt;!

            🎸 <b>ConcertBuddy</b> shows what artists play live: setlists of their concerts, every song to listen to and its lyrics.

            Send an artist or band name, or try one of these:
            ---
            Metallica → n|0|Metallica | Linkin Park → n|0|Linkin Park
            Foo Fighters → n|0|Foo Fighters | Ed Sheeran → n|0|Ed Sheeran
            ❓ Help → w | ℹ️ About → i
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Start_NoName_PlainHi(string? firstName)
    {
        _screens.Start(firstName).Html.ShouldStartWith("👋 Hi!\n\n");
    }

    [Fact]
    public void Help_WhatTheBotDoes_MenuAndAbout()
    {
        ScreenDump.Of(_screens.Help()).ShouldBe("""
            ❓ <b>How to use ConcertBuddy</b>

            🔍 <b>Search:</b> send an artist or band name, or <code>/search Metallica</code>.
            📝 <b>Setlists:</b> open the artist, then Setlists, then a concert.
            🎧 <b>Listen:</b> tap a song in a setlist or in Top tracks.
            🎼 <b>Lyrics:</b> under every track.
            📖 <b>Biography:</b> on the artist card.
            ---
            🏠 Menu → m | ℹ️ About → i
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void About_VersionSourcesSupportMenuAndHelp()
    {
        // Arrange
        var screen = _screens.About();

        // Assert
        screen.Html.ShouldStartWith($"ℹ️ <b>ConcertBuddy</b> {InfoScreens.Version}\n\n");
        screen.Html.ShouldContain("<a href=\"https://www.setlist.fm\">setlist.fm</a>");
        screen.Html.ShouldContain("<a href=\"https://github.com/skuill/ConcertBuddy\">github.com/skuill/ConcertBuddy</a>");
        screen.Keyboard.InlineKeyboard.Select(row => string.Join(" | ", row.Select(button => $"{button.Text} → {button.Url ?? button.CallbackData}")))
            .ShouldBe(new[]
            {
                "☕ Buy me a coffee → https://www.buymeacoffee.com/skuill | 💳 PayPal → https://paypal.me/VitalyKvashin",
                "🪙 Crypto → https://github.com/skuill/ConcertBuddy#support",
                "🏠 Menu → m | ❓ Help → w",
            });
    }

    [Fact]
    public void Version_ThreeParts()
    {
        InfoScreens.Version.ShouldMatch(@"^\d+\.\d+\.\d+$");
    }

    [Fact]
    public void SearchHint_HowToSearch()
    {
        ScreenDump.Of(InfoScreens.SearchHint()).ShouldBe("🔍 Add a name: <code>/search Metallica</code>, or just send the name.\n---\npreview: none");
    }
}
