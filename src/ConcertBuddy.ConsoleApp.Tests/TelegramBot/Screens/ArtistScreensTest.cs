using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.Options;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.MusicService;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class ArtistScreensTest
{
    private const string Mbid = TestArtist.MetallicaMbid;

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private readonly CallbackCodec _codec = new();

    [Fact]
    public async Task CardAsync_FromSearch_CardWithPreviewShortBiographyAndResults()
    {
        // Arrange
        var lastFm = new TestArtist
        {
            Service = MusicServiceType.LastFm,
            NameValue = "Metallica",
            BiographyValue = $"<b>Metallica</b> {new string('a', 700)} <a href=\"https://www.last.fm/music/Metallica\">Read more on Last.fm</a>",
            ExternalUrlValue = new Uri("https://www.last.fm/music/Metallica"),
        };
        Returns(new MusicArtist(new List<MusicSearcher.Model.Abstract.MusicArtistBase> { TestArtist.Metallica(), lastFm }));

        // Act
        var result = await CreateScreens(lastFmAvailable: true).CardAsync(Mbid, 5, "metallica");

        // Assert: the biography is one long word here, so it is cut right at the limit.
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            🇺🇸 <b>Metallica</b>
            Group · United States · 1981 -
            <blockquote expandable>Metallica {new string('a', ArtistScreens.MaxBiographyLength - 10)}…</blockquote>
            ---
            📝 Setlists → l|{Mbid}|1 [Primary] | 📖 Full biography → b|{Mbid}
            📻 Top tracks → o|{Mbid}
            ⬅️ Results → s|5|metallica | ❌ → /delete
            preview: https://www.last.fm/music/Metallica
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task CardAsync_NoSearchNoLastFm_OnlyMusicBrainzData()
    {
        // Arrange
        Returns(new MusicArtist(TestArtist.Metallica()));

        // Act
        var result = await CreateScreens(lastFmAvailable: false).CardAsync(Mbid, 0, "");

        // Assert
        ScreenDump.Of(result.Screen).ShouldBe($"""
            🇺🇸 <b>Metallica</b>
            Group · United States · 1981 -
            ---
            📝 Setlists → l|{Mbid}|1 [Primary] | 📖 Full biography → b|{Mbid}
            ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task CardAsync_NameOnly_NoDescriptionLine()
    {
        // Arrange
        Returns(new MusicArtist(new TestArtist { NameValue = "<Nobody>", MbidValue = Mbid }));

        // Act
        var result = await CreateScreens(lastFmAvailable: false).CardAsync(Mbid, 0, "");

        // Assert
        result.Screen.Html.ShouldBe("🎤 <b>&lt;Nobody&gt;</b>");
    }

    [Fact]
    public async Task CardAsync_MusicBrainzHasNoSuchArtist_NotFoundWithResults()
    {
        // Arrange
        Returns(new MusicArtist());

        // Act
        var result = await CreateScreens(lastFmAvailable: false).CardAsync(Mbid, 0, "metallica");

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        ScreenDump.Of(result.Screen).ShouldBe("""
            🤷 MusicBrainz does not have this artist any more. Try the search again.
            ---
            ⬅️ Results → s|0|metallica | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task CardAsync_MusicBrainzDown_TryAgainTheSameCard()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new TimeoutException()));

        // Act
        var result = await CreateScreens(lastFmAvailable: false).CardAsync(Mbid, 5, "metallica");

        // Assert
        result.Result.ShouldBe(CommandResult.Unavailable);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            ⚠️ MusicBrainz is busy or down right now. It is not your query: try again in a minute.
            ---
            🔄 Try again → a|{Mbid}|5|metallica [Primary] | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task BiographyAsync_FullBiography_RichParagraphsAndLastFmCredit()
    {
        // Arrange
        var lastFm = new TestArtist
        {
            Service = MusicServiceType.LastFm,
            BiographyValue = "Short summary.",
            FullBiographyValue = "Metallica is a <b>band</b> &amp; more.\n\nFormed in 1981.  \n<a href=\"https://www.last.fm/music/Metallica\">Read more on Last.fm</a>. User-contributed text is available under the Creative Commons By-SA License; additional terms may apply.",
            ExternalUrlValue = new Uri("https://www.last.fm/music/Metallica"),
        };
        Returns(new MusicArtist(new List<MusicSearcher.Model.Abstract.MusicArtistBase> { TestArtist.Metallica(), lastFm }));

        // Act
        var result = await CreateScreens(lastFmAvailable: true).BiographyAsync(Mbid);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        result.Screen.IsRich.ShouldBeTrue();
        ScreenDump.Of(result.Screen).ShouldBe($"""
            <h3>📖 Metallica</h3><p>Metallica is a band &amp; more.</p><p>Formed in 1981.</p><footer>Biography from <a href="https://www.last.fm/music/Metallica">Last.fm</a>, user-contributed text under CC BY-SA</footer>
            ---
            ⬅️ Artist → a|{Mbid}|0|
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task BiographyAsync_OnlySummary_SummaryWithoutCredit()
    {
        // Arrange: no Last.fm page, the summary comes from another service.
        Returns(new MusicArtist(new TestArtist { NameValue = "Metallica", BiographyValue = "Short summary." }));

        // Act
        var result = await CreateScreens(lastFmAvailable: false).BiographyAsync(Mbid);

        // Assert
        result.Screen.Html.ShouldBe("<h3>📖 Metallica</h3><p>Short summary.</p>");
    }

    [Fact]
    public async Task BiographyAsync_NoBiography_NotFoundPlainScreen()
    {
        // Arrange
        Returns(new MusicArtist(TestArtist.Metallica()));

        // Act
        var result = await CreateScreens(lastFmAvailable: false).BiographyAsync(Mbid);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        result.Screen.IsRich.ShouldBeFalse();
        result.Screen.Html.ShouldBe("📖 Last.fm has no biography of <b>Metallica</b> yet.");
    }

    [Fact]
    public async Task BiographyAsync_MusicBrainzDown_TryAgainTheBiography()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new TimeoutException()));

        // Act
        var result = await CreateScreens(lastFmAvailable: false).BiographyAsync(Mbid);

        // Assert
        result.Result.ShouldBe(CommandResult.Unavailable);
        result.Screen.Keyboard.InlineKeyboard.First().First().CallbackData.ShouldBe($"b|{Mbid}");
    }

    private void Returns(MusicArtist artist)
    {
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).Returns(artist);
    }

    private ArtistScreens CreateScreens(bool lastFmAvailable)
    {
        var configuration = lastFmAvailable ? new Configuration { LastFmApiKey = "key", LastFmApiSecret = "secret" } : new Configuration();
        return new ArtistScreens(_music, _codec, new SearchScreens(_music, _codec), Options.Create(configuration));
    }
}
