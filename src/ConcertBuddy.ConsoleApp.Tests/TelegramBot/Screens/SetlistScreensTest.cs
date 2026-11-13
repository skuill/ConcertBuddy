using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using FakeItEasy;
using MusicSearcher;
using MusicSearcher.Model;
using Shouldly;
using System.Text;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class SetlistScreensTest
{
    private const string Mbid = TestArtist.MetallicaMbid;
    private const string SetlistId = "4b48efd6";

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private readonly SetlistScreens _screens;

    public SetlistScreensTest()
    {
        _screens = new SetlistScreens(_music, new CallbackCodec());
    }

    [Fact]
    public async Task SetlistsAsync_FirstScreen_TenSetlistsMarksAndPages()
    {
        // Arrange: 25 setlists are three screens; the first API page holds twenty.
        var setlists = Enumerable.Range(1, 20).Select(day => Setlist($"id{day}", $"{day:D2}-03-2001", withSongs: day != 2)).ToList();
        setlists[0].EventDate = "01-01-2999";
        Returns(1, new MusicSetlists { Setlist = setlists, Total = 25, Page = 1, ItemsPerPage = 20 });

        // Act
        var result = await _screens.SetlistsAsync(Mbid, 1);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            📝 <b>Metallica</b> · 25 setlists
            🔜 upcoming · 📭 no songs on setlist.fm yet
            ---
            🔜 1 Jan 2999 · Stockholm · Friends Arena → t|id1|{Mbid}|1
            📭 2 Mar 2001 · Stockholm · Friends Arena → t|id2|{Mbid}|1
            3 Mar 2001 · Stockholm · Friends Arena → t|id3|{Mbid}|1
            4 Mar 2001 · Stockholm · Friends Arena → t|id4|{Mbid}|1
            5 Mar 2001 · Stockholm · Friends Arena → t|id5|{Mbid}|1
            6 Mar 2001 · Stockholm · Friends Arena → t|id6|{Mbid}|1
            7 Mar 2001 · Stockholm · Friends Arena → t|id7|{Mbid}|1
            8 Mar 2001 · Stockholm · Friends Arena → t|id8|{Mbid}|1
            9 Mar 2001 · Stockholm · Friends Arena → t|id9|{Mbid}|1
            10 Mar 2001 · Stockholm · Friends Arena → t|id10|{Mbid}|1
            ⬅️ [disabled] | 1 / 3 [disabled] | ➡️ → l|{Mbid}|2
            ⬅️ Artist → a|{Mbid}|0|
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SetlistsAsync_SecondScreen_SecondHalfOfTheFirstApiPage()
    {
        // Arrange
        var setlists = Enumerable.Range(1, 20).Select(day => Setlist($"id{day}", $"{day:D2}-03-2001")).ToList();
        Returns(1, new MusicSetlists { Setlist = setlists, Total = 20 });

        // Act
        var result = await _screens.SetlistsAsync(Mbid, 2);

        // Assert: the last screen, the forward arrow is disabled.
        var rows = result.Screen.Keyboard.InlineKeyboard.Select(row => row.ToList()).ToList();
        rows[0].Single().CallbackData.ShouldBe($"t|id11|{Mbid}|2");
        rows[9].Single().CallbackData.ShouldBe($"t|id20|{Mbid}|2");
        rows[10].Select(button => (button.Text, button.CallbackData, button.Disabled != null))
            .ShouldBe(new (string, string?, bool)[] { ("⬅️", $"l|{Mbid}|1", false), ("2 / 2", null, true), ("➡️", null, true) });
    }

    [Fact]
    public async Task SetlistsAsync_ThirdScreen_SecondApiPage()
    {
        // Arrange
        Returns(2, new MusicSetlists { Setlist = new List<MusicSetlist> { Setlist("id21", "21-03-2001") }, Total = 21 });

        // Act
        var result = await _screens.SetlistsAsync(Mbid, 3);

        // Assert
        result.Screen.Keyboard.InlineKeyboard.First().Single().CallbackData.ShouldBe($"t|id21|{Mbid}|3");
        A.CallTo(() => _music.SearchArtistSetlists(Mbid, 2)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SetlistsAsync_NoSetlists_NotFoundAndBackToTheArtist()
    {
        // Arrange
        Returns(1, null);

        // Act
        var result = await _screens.SetlistsAsync(Mbid, 1);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            📭 No setlists of this artist on setlist.fm yet.
            ---
            ⬅️ Artist → a|{Mbid}|0|
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SetlistsAsync_SetlistFmDown_TryAgainTheSameScreen()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistSetlists(Mbid, 1)).ThrowsAsync(new MusicServiceUnavailableException("setlist.fm", new HttpRequestException("500")));

        // Act
        var result = await _screens.SetlistsAsync(Mbid, 2);

        // Assert
        result.Result.ShouldBe(CommandResult.Unavailable);
        result.Screen.Keyboard.InlineKeyboard.First().First().CallbackData.ShouldBe($"l|{Mbid}|2");
        result.Screen.Html.ShouldStartWith("⚠️ setlist.fm is busy or down right now.");
    }

    [Fact]
    public async Task SetlistAsync_FullSetlist_RichTableWithSongButtonsNotesAndLink()
    {
        // Arrange
        var setlist = Setlist(SetlistId, "31-10-2001", withSongs: false);
        setlist.TourName = "M72 <World> Tour";
        setlist.Info = "Rain & thunder";
        setlist.Url = "https://www.setlist.fm/setlist/metallica/2001/x.html";
        setlist.Sets = new MusicSets
        {
            Set = new List<MusicSet>
            {
                new() { Songs = new List<MusicSong> { new() { Name = "The Ecstasy of Gold", Tape = true }, new() { Name = "Creeping Death" }, new() { Name = "Whiskey in the Jar", CoverName = "Thin Lizzy" } } },
                new() { Encore = 1, Songs = new List<MusicSong> { new() { Name = "Seek & Destroy", WithName = "Jason", Info = "Extended" } } },
            },
        };
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns(setlist);

        // Act
        var result = await _screens.SetlistAsync(SetlistId, Mbid, 3);

        // Assert
        result.Result.ShouldBe(CommandResult.Found);
        result.Screen.IsRich.ShouldBeTrue();
        result.Screen.Html.ShouldBe(
            "<h3>Metallica</h3>"
            + "<p><b>M72 &lt;World&gt; Tour</b></p>"
            + "<p>📍 Friends Arena, Stockholm, Sweden · 31 Oct 2001</p>"
            + "<p><i>3 songs · 1 cover · 1 with guests · encore</i></p>"
            + "<table bordered striped><tr><th>#</th><th>Song</th><th>Note</th></tr>"
            + "<tr><td></td><td><i>The Ecstasy of Gold</i></td><td>📼 from tape</td></tr>"
            + $"<tr><td>1</td><td><tg-button type=\"callback_data\" style=\"link\" data=\"/track {Mbid} Creeping Death\">Creeping Death</tg-button></td><td></td></tr>"
            + $"<tr><td>2</td><td><tg-button type=\"callback_data\" style=\"link\" data=\"/track {Mbid} Whiskey in the Jar\">Whiskey in the Jar</tg-button></td><td>🎸 Thin Lizzy cover</td></tr>"
            + "<tr><td colspan=\"3\"><b>🔁 Encore</b></td></tr>"
            + $"<tr><td>3</td><td><tg-button type=\"callback_data\" style=\"link\" data=\"/track {Mbid} Seek &amp; Destroy\">Seek &amp; Destroy</tg-button></td><td>👥 with Jason; ℹ️ Extended</td></tr>"
            + "</table>"
            + "<blockquote>ℹ️ Rain &amp; thunder</blockquote>"
            + "<footer>Tap a song to listen · Setlist from <a href=\"https://www.setlist.fm/setlist/metallica/2001/x.html\">setlist.fm</a></footer>");
        result.Screen.Keyboard.InlineKeyboard.First().Single().CopyText!.Text.ShouldBe("Metallica · 31 Oct 2001 · Stockholm\n1. Creeping Death\n2. Whiskey in the Jar\n3. Seek & Destroy");
        result.Screen.Keyboard.InlineKeyboard.Last().Single().CallbackData.ShouldBe($"l|{Mbid}|3");
    }

    [Fact]
    public async Task SetlistAsync_NoNotes_TwoColumnsAndEncoreNumbers()
    {
        // Arrange
        var setlist = Setlist(SetlistId, "31-10-2001", withSongs: false);
        setlist.Sets = new MusicSets
        {
            Set = new List<MusicSet>
            {
                new() { Name = "Acoustic", Songs = new List<MusicSong> { new() { Name = "One" } } },
                new() { Encore = 2, Songs = new List<MusicSong> { new() { Name = "Two" } } },
            },
        };
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns(setlist);

        // Act
        var html = (await _screens.SetlistAsync(SetlistId, Mbid, 1)).Screen.Html;

        // Assert
        html.ShouldContain("<p><i>2 songs · encore</i></p><table bordered striped><tr><th>#</th><th>Song</th></tr><tr><td colspan=\"2\"><b>🔁 Acoustic</b></td></tr>");
        html.ShouldContain("<tr><td colspan=\"2\"><b>🔁 Encore 2</b></td></tr>");
    }

    [Fact]
    public async Task SetlistAsync_LongSetlist_CopyTextFitsTheButtonInBytesWithWholeLines()
    {
        // Arrange: 30 Cyrillic names take two bytes per letter, far over 256 bytes, and less than 256 characters at 13 songs.
        var setlist = Setlist(SetlistId, "31-10-2001", withSongs: false);
        setlist.Sets = new MusicSets { Set = new List<MusicSet> { new() { Songs = Enumerable.Range(1, 30).Select(n => new MusicSong { Name = $"Песня {n}" }).ToList() } } };
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns(setlist);

        // Act
        var copy = (await _screens.SetlistAsync(SetlistId, Mbid, 1)).Screen.Keyboard.InlineKeyboard.First().Single().CopyText!.Text;

        // Assert: within the limit, whole song lines, and the next song would not fit any more.
        Encoding.UTF8.GetByteCount(copy).ShouldBeLessThanOrEqualTo(SetlistScreens.MaxCopyTextBytes);
        copy.ShouldEndWith("\n…");
        var lines = copy.Split('\n')[1..^1];
        lines.ShouldBe(Enumerable.Range(1, lines.Length).Select(n => $"{n}. Песня {n}"));
        Encoding.UTF8.GetByteCount(copy + $"\n{lines.Length + 1}. Песня {lines.Length + 1}").ShouldBeGreaterThan(SetlistScreens.MaxCopyTextBytes);
    }

    [Theory]
    [InlineData("01-01-2999", "<p>🔜 Upcoming concert: no setlist yet.</p>")]
    [InlineData("01-01-2001", "<p>📭 Nobody has added the songs on setlist.fm yet.</p>")]
    public async Task SetlistAsync_NoSongs_SaysWhyWithoutCopyButton(string date, string expected)
    {
        // Arrange
        var setlist = Setlist(SetlistId, date, withSongs: false);
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns(setlist);

        // Act
        var screen = (await _screens.SetlistAsync(SetlistId, Mbid, 1)).Screen;

        // Assert
        screen.Html.ShouldContain(expected);
        screen.Html.ShouldContain("<footer>Setlist from <a href=\"https://www.setlist.fm/search?query=Metallica\">setlist.fm</a></footer>");
        screen.Keyboard.InlineKeyboard.Single().Single().Text.ShouldBe("⬅️ Setlists");
    }

    [Fact]
    public async Task SetlistAsync_Gone_NotFoundAndBack()
    {
        // Arrange
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns((MusicSetlist?)null);

        // Act
        var result = await _screens.SetlistAsync(SetlistId, Mbid, 2);

        // Assert
        result.Result.ShouldBe(CommandResult.NotFound);
        ScreenDump.Of(result.Screen).ShouldBe($"""
            🤷 setlist.fm does not have this setlist any more.
            ---
            ⬅️ Setlists → l|{Mbid}|2
            preview: none
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task SetlistAsync_SetlistFmDown_TryAgainTheSameSetlist()
    {
        // Arrange
        A.CallTo(() => _music.SearchSetlist(SetlistId)).ThrowsAsync(new MusicServiceUnavailableException("setlist.fm", new HttpRequestException("500")));

        // Act
        var result = await _screens.SetlistAsync(SetlistId, Mbid, 2);

        // Assert
        result.Result.ShouldBe(CommandResult.Unavailable);
        result.Screen.Keyboard.InlineKeyboard.First().First().CallbackData.ShouldBe($"t|{SetlistId}|{Mbid}|2");
    }

    private void Returns(int apiPage, MusicSetlists? setlists)
    {
        A.CallTo(() => _music.SearchArtistSetlists(Mbid, apiPage)).Returns(setlists);
    }

    private static MusicSetlist Setlist(string id, string date, bool withSongs = true)
    {
        return new MusicSetlist
        {
            Id = id,
            EventDate = date,
            ArtistMBID = Mbid,
            ArtistName = "Metallica",
            VenueName = "Friends Arena",
            CityName = "Stockholm",
            CountryName = "Sweden",
            Sets = withSongs ? new MusicSets { Set = new List<MusicSet> { new() { Songs = new List<MusicSong> { new() { Name = "One" } } } } } : null,
        };
    }
}
