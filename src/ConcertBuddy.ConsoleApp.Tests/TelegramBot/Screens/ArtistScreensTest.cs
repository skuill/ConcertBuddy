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
            🎓 Biography → /biography {Mbid} | 📝 Setlists → /setlists 1 1 {Mbid}
            📻 Top tracks → /top track {Mbid}
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
            🎓 Biography → /biography {Mbid} | 📝 Setlists → /setlists 1 1 {Mbid}
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
