using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using FakeItEasy;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using Shouldly;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class TrackScreensTest
{
    private const string Mbid = TestArtist.MetallicaMbid;

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private readonly CallbackCodec _codec = new();
    private readonly TrackScreens _screens;

    public TrackScreensTest()
    {
        _screens = new TrackScreens(_music, _codec);
    }

    [Fact]
    public void Track_WithAudio_PlayerWithCaptionAndButtons()
    {
        // Arrange
        var audio = new InputMediaAudio(InputFile.FromFileId("file-1"));

        // Act
        var screen = _screens.Track(Mbid, "AC/DC", "T.N.T. & <more>", audio, lyricsHtml: null);

        // Assert
        screen.IsRich.ShouldBeTrue();
        screen.Media!.Single().Id.ShouldBe(TrackScreens.AudioMediaId);
        screen.Media!.Single().Media.ShouldBeSameAs(audio);
        ScreenDump.Of(screen).ShouldBe($"""
            <figure><audio src="tg://audio?id=track"></audio><figcaption>AC/DC – T.N.T. &amp; &lt;more&gt;</figcaption></figure>
            ---
            🎼 Lyrics → y|{Mbid}|T.N.T. & <more> | 📋 Copy name | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
        screen.Keyboard.InlineKeyboard.Single().ElementAt(1).CopyText!.Text.ShouldBe("AC/DC – T.N.T. & <more>");
    }

    [Fact]
    public void Track_NoAudioWithLyrics_HeadingAndHideButton()
    {
        // Act
        var screen = _screens.Track(Mbid, "Metallica", "One", audio: null, lyricsHtml: "<p>lyrics</p>");

        // Assert
        screen.Media.ShouldBeNull();
        ScreenDump.Of(screen).ShouldBe($"""
            <h3>Metallica – One</h3><p>🔇 No audio for this song.</p><p>lyrics</p>
            ---
            🎼 Hide lyrics → h|{Mbid}|One | 📋 Copy name | ❌ → /delete
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Track_NoArtistName_SongAlone()
    {
        // Act
        var screen = _screens.Track(Mbid, string.Empty, "One", audio: null, lyricsHtml: null);

        // Assert
        screen.Html.ShouldStartWith("<h3>One</h3>");
        screen.Keyboard.InlineKeyboard.Single().ElementAt(1).CopyText!.Text.ShouldBe("One");
    }

    [Fact]
    public async Task LyricsAsync_Found_LinesWithBreaksUnderADivider()
    {
        // Arrange: MusicBrainz knows the exact title, the lyrics are searched by it.
        A.CallTo(() => _music.SearchRecordByName(Mbid, "seek and destroy")).Returns(new MusicRecording { Title = "Seek & Destroy" });
        A.CallTo(() => _music.SearchLyric("Metallica", "Seek & Destroy")).Returns(new MusicLyric { IsSuccessSearchResult = true, LyricText = "\nScanning <the> scene\r\nIn the city tonight\n" });

        // Act
        var (html, result) = await _screens.LyricsAsync(Mbid, "Metallica", "seek and destroy");

        // Assert
        result.ShouldBe(CommandResult.Found);
        html.ShouldBe("<hr/><p>Scanning &lt;the&gt; scene<br/>In the city tonight</p>");
    }

    [Fact]
    public async Task LyricsAsync_NoRecording_SearchedByTheSongName()
    {
        // Arrange
        A.CallTo(() => _music.SearchRecordByName(Mbid, "One")).Returns((MusicRecording?)null);
        A.CallTo(() => _music.SearchLyric("Metallica", "One")).Returns(new MusicLyric { IsSuccessSearchResult = true, LyricText = "I can't remember anything" });

        // Act
        var (html, _) = await _screens.LyricsAsync(Mbid, "Metallica", "One");

        // Assert
        html.ShouldBe("<hr/><p>I can't remember anything</p>");
    }

    [Fact]
    public async Task LyricsAsync_Instrumental_SaysSo()
    {
        // Arrange
        A.CallTo(() => _music.SearchLyric(A<string>._, A<string>._)).Returns(new MusicLyric { IsSuccessSearchResult = true, Instrumental = true });

        // Act
        var (html, result) = await _screens.LyricsAsync(Mbid, "Metallica", "Orion");

        // Assert
        result.ShouldBe(CommandResult.Found);
        html.ShouldBe("<p>🎼 Instrumental, no lyrics.</p>");
    }

    [Theory]
    [InlineData(false, "lyrics")]
    [InlineData(true, " ")]
    public async Task LyricsAsync_NotFound_SaysSo(bool success, string text)
    {
        // Arrange
        A.CallTo(() => _music.SearchLyric(A<string>._, A<string>._)).Returns(new MusicLyric { IsSuccessSearchResult = success, LyricText = text });

        // Act
        var (html, result) = await _screens.LyricsAsync(Mbid, "Metallica", "Intro");

        // Assert
        result.ShouldBe(CommandResult.NotFound);
        html.ShouldBe("<p>🎼 No lyrics found for this song.</p>");
    }

    [Fact]
    public async Task LyricsAsync_VeryLong_Shortened()
    {
        // Arrange
        A.CallTo(() => _music.SearchLyric(A<string>._, A<string>._)).Returns(new MusicLyric { IsSuccessSearchResult = true, LyricText = new string('a', 50_000) });

        // Act
        var (html, _) = await _screens.LyricsAsync(Mbid, "Metallica", "Long");

        // Assert
        html.Length.ShouldBe("<hr/><p></p>".Length + TrackScreens.MaxLyricsLength + 1);
    }

    [Fact]
    public async Task TopTracksAsync_Tracks_NumberedPlayButtonsAndBack()
    {
        // Arrange
        A.CallTo(() => _music.SearchTopTracks(Mbid)).Returns(new[] { Track("Enter Sandman"), Track(null), Track("One") });

        // Act
        var result = await _screens.TopTracksAsync(Mbid);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            📻 <b>Top tracks</b> on Last.fm, tap one to listen:
            ---
            1. Enter Sandman → p|{Mbid}|Enter Sandman
            2. One → p|{Mbid}|One
            ⬅️ Artist → a|{Mbid}|0|
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task TopTracksAsync_None_NotFoundAndBack()
    {
        // Arrange
        A.CallTo(() => _music.SearchTopTracks(Mbid)).Returns(new List<MusicTrackBase>());

        // Act
        var result = await _screens.TopTracksAsync(Mbid);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            📻 No top tracks of this artist on Last.fm yet.
            ---
            ⬅️ Artist → a|{Mbid}|0|
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task TopTracksAsync_MusicBrainzDown_TryAgain()
    {
        // Arrange
        A.CallTo(() => _music.SearchTopTracks(Mbid)).ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new TimeoutException()));

        // Act
        var result = await _screens.TopTracksAsync(Mbid);

        // Assert
        result.Result.ShouldBe(CommandResult.Unavailable);
        result.Screen.Keyboard.InlineKeyboard.First().First().CallbackData.ShouldBe($"o|{Mbid}");
    }

    [Fact]
    public void Unavailable_TryAgainPlaysAgain()
    {
        // Act
        var screen = _screens.Unavailable("MusicBrainz", Mbid, "One");

        // Assert
        screen.Keyboard.InlineKeyboard.Single().First().CallbackData.ShouldBe($"q|{Mbid}|One");
    }

    [Theory]
    [InlineData("Metallica", "One", "Metallica - One.mp3")]
    [InlineData("AC/DC", "T.N.T.", "AC_DC - T.N.T..mp3")]
    [InlineData("Who?", "Baba O'Riley: \"Live\"", "Who_ - Baba O'Riley_ _Live_.mp3")]
    public void FileName_Names_SafeForFiles(string artist, string song, string expected)
    {
        TrackScreens.FileName(artist, song).ShouldBe(expected);
    }

    private static MusicTrackBase Track(string? name)
    {
        var track = A.Fake<MusicTrackBase>();
        A.CallTo(() => track.TrackName).Returns(name);
        return track;
    }
}
