using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using FakeItEasy;
using MusicSearcher;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicBrainz;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class SearchScreensTest
{
    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private readonly CallbackCodec _codec = new();
    private readonly SearchScreens _screens;

    public SearchScreensTest()
    {
        _screens = new SearchScreens(_music, _codec);
    }

    [Fact]
    public async Task SearchAsync_FullPage_ListWithNextPage()
    {
        // Arrange
        Returns("Metallica", 0, TestArtist.Metallica(), TestArtist.Numbered(2), TestArtist.Numbered(3), TestArtist.Numbered(4), TestArtist.Numbered(5));

        // Act
        var result = await _screens.SearchAsync("Metallica", 0);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe("""
            Choose the artist for <b>Metallica</b>:
            ---
            🇺🇸 Metallica · Group · United States · 1981 - → a|65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab|0|Metallica
            🎤 Artist 2 · Person → a|00000000-0000-0000-0000-000000000002|0|Metallica
            🎤 Artist 3 · Person → a|00000000-0000-0000-0000-000000000003|0|Metallica
            🎤 Artist 4 · Person → a|00000000-0000-0000-0000-000000000004|0|Metallica
            🎤 Artist 5 · Person → a|00000000-0000-0000-0000-000000000005|0|Metallica
            ❌ → /delete | ➡️ → s|5|Metallica
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SearchAsync_LastPage_PageNumberAndBackButton()
    {
        // Arrange
        Returns("Metallica", 5, TestArtist.Numbered(6));

        // Act
        var result = await _screens.SearchAsync("Metallica", 5);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe("""
            Choose the artist for <b>Metallica</b> (page 2):
            ---
            🎤 Artist 6 · Person → a|00000000-0000-0000-0000-000000000006|5|Metallica
            ⬅️ → s|0|Metallica | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SearchAsync_NothingFound_TipsAndExamples()
    {
        // Arrange
        Returns("<Metalica>", 0);

        // Act
        var result = await _screens.SearchAsync("<Metalica>", 0);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        ScreenDump.Of(result.Screen).ShouldBe("""
            🤷 Nothing found for <b>&lt;Metalica&gt;</b>.

            • Check the spelling
            • Send the name of the band, not of a song
            • Try the original spelling (Кино, not Kino)

            Or try one of these:
            ---
            Metallica → n|0|Metallica | Linkin Park → n|0|Linkin Park
            Foo Fighters → n|0|Foo Fighters | Ed Sheeran → n|0|Ed Sheeran
            ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SearchAsync_EmptyNextPage_NoMoreAndBack()
    {
        // Arrange
        Returns("Metallica", 10);

        // Act
        var result = await _screens.SearchAsync("Metallica", 10);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        ScreenDump.Of(result.Screen).ShouldBe("""
            No more artists for <b>Metallica</b>.
            ---
            ⬅️ → s|5|Metallica | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SearchAsync_MusicBrainzDown_TryAgainTheSamePage()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistsByName("Metallica", A<ScoreType>._, 5, 5))
            .ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new HttpRequestException("503")));

        // Act
        var result = await _screens.SearchAsync("Metallica", 5);

        // Assert
        result.Result.ShouldBe(CommandResult.Unavailable);
        ScreenDump.Of(result.Screen).ShouldBe("""
            ⚠️ MusicBrainz is busy or down right now. It is not your query: try again in a minute.
            ---
            🔄 Try again → n|5|Metallica [Primary] | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SearchAsync_OtherError_NotHidden()
    {
        // Arrange: a bug is not "service unavailable", the update handler logs it as an error.
        A.CallTo(() => _music.SearchArtistsByName(A<string>._, A<ScoreType>._, A<int>._, A<int>._)).ThrowsAsync(new InvalidOperationException("bug"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _screens.SearchAsync("Metallica", 0));
    }

    [Fact]
    public async Task SearchAsync_LongText_ButtonsKeepItInMemory()
    {
        // Arrange
        var text = "The Very Long Name Of A Band That Does Not Fit Into The Button";
        Returns(text, 0, TestArtist.Metallica());

        // Act
        var result = await _screens.SearchAsync(text, 0);

        // Assert
        var data = result.Screen.Keyboard.InlineKeyboard.First().Single().CallbackData!;
        data.ShouldStartWith("~");
        _codec.TryDecode(data, out var payload).ShouldBeTrue();
        payload.ShouldBe(new CallbackPayload(CallbackActions.Artist, $"{TestArtist.MetallicaMbid}|0|{text}"));
    }

    [Fact]
    public void Searching_Text_Encoded()
    {
        ScreenDump.Of(SearchScreens.Searching("AC&DC")).ShouldBe("🔍 Searching for <b>AC&amp;DC</b>…\n---\npreview: none");
    }

    [Theory]
    [InlineData("  Metallica  ", "Metallica")]
    [InlineData("", "")]
    public void CleanText_Spaces_Trimmed(string text, string expected)
    {
        SearchScreens.CleanText(text).ShouldBe(expected);
    }

    [Fact]
    public void CleanText_VeryLong_Shortened()
    {
        SearchScreens.CleanText(new string('x', 500)).Length.ShouldBe(SearchScreens.MaxSearchTextLength + 1);
    }

    private void Returns(string text, int offset, params MusicArtistBase[] artists)
    {
        A.CallTo(() => _music.SearchArtistsByName(text, A<ScoreType>._, SearchScreens.PageSize, offset)).Returns(artists);
    }
}
