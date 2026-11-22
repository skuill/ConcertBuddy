using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.Options;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicBrainz;
using MusicSearcher.MusicService;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class SearchScreensTest
{
    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private readonly CallbackCodec _codec = new();
    private readonly SearchScreens _screens;

    public SearchScreensTest()
    {
        _screens = new SearchScreens(_music, _codec, new ArtistScreens(_music, _codec, Options.Create(new Configuration())));
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

    [Theory]
    [InlineData("Metallica")]
    [InlineData("  metallica ")]
    [InlineData("METALLICA!")]
    public async Task SearchAsync_NewSearchOneArtistWithTheName_CardAtOnceWithNotThisArtist(string text)
    {
        // Arrange
        Returns(text, 0, TestArtist.Metallica(), TestArtist.Numbered(2));
        A.CallTo(() => _music.SearchArtistByMBID(TestArtist.MetallicaMbid)).Returns(new MusicArtist(TestArtist.Metallica()));

        // Act
        var result = await _screens.SearchAsync(text, 0, newSearch: true);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            🇺🇸 <b>Metallica</b>
            Group · United States · 1981 -
            ---
            📝 Setlists → l|{TestArtist.MetallicaMbid}|1 [Primary] | 📅 Upcoming → u|{TestArtist.MetallicaMbid}
            📖 Full biography → b|{TestArtist.MetallicaMbid}
            🔄 Not this artist? → s|0|{text} | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SearchAsync_NewSearchTwoArtistsWithTheName_ListWithoutTheCard()
    {
        // Arrange
        Returns("Metallica", 0, TestArtist.Metallica(), TestArtist.Namesake(2));

        // Act
        var result = await _screens.SearchAsync("Metallica", 0, newSearch: true);

        // Assert
        result.Screen.Html.ShouldBe("Choose the artist for <b>Metallica</b>:");
        A.CallTo(() => _music.SearchArtistByMBID(A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SearchAsync_PageOrBackToTheResults_ListEvenWithOneArtistOfTheName()
    {
        // Arrange: "Not this artist?" and "Results" lead here, they must not open the card again.
        Returns("Metallica", 0, TestArtist.Metallica(), TestArtist.Numbered(2));

        // Act
        var result = await _screens.SearchAsync("Metallica", 0);

        // Assert
        result.Screen.Html.ShouldBe("Choose the artist for <b>Metallica</b>:");
        A.CallTo(() => _music.SearchArtistByMBID(A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SearchAsync_NothingInTheTypedLayout_DidYouMeanListOfTheOtherLayout()
    {
        // Arrange
        Returns("ьуефддшсф", 0);
        Returns("metallica", 0, TestArtist.Metallica(), TestArtist.Numbered(2));

        // Act
        var result = await _screens.SearchAsync("ьуефддшсф", 0, newSearch: true);

        // Assert: the list of the swapped text, its buttons lead back to it; no card at once, the user did not type it.
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe("""
            🤔 Nothing found for <b>ьуефддшсф</b>. Did you mean <b>metallica</b>?
            ---
            🇺🇸 Metallica · Group · United States · 1981 - → a|65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab|0|metallica
            🎤 Artist 2 · Person → a|00000000-0000-0000-0000-000000000002|0|metallica
            ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SearchAsync_NothingInBothLayouts_NotFoundForTheTypedText()
    {
        // Arrange
        Returns("Metalica", 0);
        Returns("ьуефдшсф", 0);

        // Act
        var result = await _screens.SearchAsync("Metalica", 0, newSearch: true);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        result.Screen.Html.ShouldStartWith("🤷 Nothing found for <b>Metalica</b>.");
        A.CallTo(() => _music.SearchArtistsByName("ьуефдшсф", A<ScoreType>._, SearchScreens.PageSize, 0)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SearchAsync_PageWithNothing_NoSecondLayout()
    {
        // Arrange
        Returns("Metalica", 0);

        // Act
        var result = await _screens.SearchAsync("Metalica", 0);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        A.CallTo(() => _music.SearchArtistsByName("ьуефдшсф", A<ScoreType>._, A<int>._, A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SearchAsync_OtherLayoutBusy_TryAgainTheTypedText()
    {
        // Arrange
        Returns("ьуефддшсф", 0);
        A.CallTo(() => _music.SearchArtistsByName("metallica", A<ScoreType>._, SearchScreens.PageSize, 0))
            .ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new HttpRequestException("503")));

        // Act
        var result = await _screens.SearchAsync("ьуефддшсф", 0, newSearch: true);

        // Assert
        result.Result.ShouldBe(CommandResult.Unavailable);
        result.Screen.Keyboard.InlineKeyboard.First().First().CallbackData.ShouldBe("n|0|ьуефддшсф");
    }

    [Fact]
    public async Task SearchAsync_MusicBrainzBusyListFromLastFm_NoteUnderTheTitle()
    {
        // Arrange: MusicSearcherClient gave the artists of Last.fm (#91): names and MBIDs only.
        Returns("Metallica", 0,
            new MusicArtist(new TestArtist { Service = MusicServiceType.LastFm, NameValue = "Metallica", MbidValue = TestArtist.MetallicaMbid, ListenersValue = 3_142_000 }),
            new MusicArtist(new TestArtist { Service = MusicServiceType.LastFm, NameValue = "Metallica Tribute", MbidValue = "00000000-0000-0000-0000-000000000002", ListenersValue = 850 }));

        // Act
        var result = await _screens.SearchAsync("Metallica", 0);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe("""
            Choose the artist for <b>Metallica</b>:
            <i>MusicBrainz is busy, so this comes from Last.fm.</i>
            ---
            🎤 Metallica · 3.1M listeners → a|65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab|0|Metallica
            🎤 Metallica Tribute · 850 listeners → a|00000000-0000-0000-0000-000000000002|0|Metallica
            ❌ → /delete
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
