using ConcertBuddy.ConsoleApp.TelegramBot;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using FakeItEasy;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using Shouldly;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Helper;

public sealed class InlineKeyboardHelperTest
{
    private const string Mbid = "827e1b52-1782-4e73-b8e7-a7b6939370f5";
    private const int CallbackDataMaxBytes = 64;

    public static TheoryData<string> LongNames => new()
    {
        new string('a', 100),
        new string('Я', 60),
        string.Concat(Enumerable.Repeat("🎸", 30)),
        "Стволок за поясок и ещё немного русского текста 🎸 with mixed English",
    };

    [Theory]
    [MemberData(nameof(LongNames))]
    public void GetLyricInlineKeyboardMenu_LongName_CallbackDataFitsTelegramLimit(string name)
    {
        // Act
        var result = InlineKeyboardHelper.GetLyricInlineKeyboardMenu(Mbid, name);

        // Assert
        AssertCallbackDataFits(result);
    }

    [Theory]
    [MemberData(nameof(LongNames))]
    public void GetTracksInlineKeyboardMenu_SetWithLongName_CallbackDataFitsTelegramLimit(string name)
    {
        // Arrange
        var set = new MusicSet { Songs = new List<MusicSong> { new() { Name = name } } };

        // Act
        var result = InlineKeyboardHelper.GetTracksInlineKeyboardMenu(set, Mbid);

        // Assert
        AssertCallbackDataFits(result);
    }

    [Theory]
    [MemberData(nameof(LongNames))]
    public void GetTracksInlineKeyboardMenu_TracksWithLongName_CallbackDataFitsTelegramLimit(string name)
    {
        // Act
        var result = InlineKeyboardHelper.GetTracksInlineKeyboardMenu(new[] { CreateTrack(name) }, Mbid);

        // Assert
        AssertCallbackDataFits(result);
    }

    [Fact]
    public void GetLyricInlineKeyboardMenu_ShortName_HasLyricAndDeleteButtons()
    {
        // Act
        var result = InlineKeyboardHelper.GetLyricInlineKeyboardMenu(Mbid, "One");

        // Assert
        Buttons(result).ShouldBe(new[]
        {
            ("🎼 Lyric", string.Format(CommandList.CALLBACK_DATA_FORMAT_LYRIC, Mbid, "One")),
            ("❌", CommandList.CALLBACK_DATA_FORMAT_DELETE),
        });
    }

    [Fact]
    public void GetTracksInlineKeyboardMenu_Set_NumbersSongsAndKeepsSongNameInCallback()
    {
        // Arrange
        var set = new MusicSet
        {
            Songs = new List<MusicSong>
            {
                new() { Name = "One" },
                new() { Name = "Whiskey in the Jar", CoverName = "[traditional]" },
            },
        };

        // Act
        var result = InlineKeyboardHelper.GetTracksInlineKeyboardMenu(set, Mbid);

        // Assert
        Buttons(result).ShouldBe(new[]
        {
            ("1. One", string.Format(CommandList.CALLBACK_DATA_FORMAT_TRACK, Mbid, "One")),
            ("2. Whiskey in the Jar ([traditional] cover)", string.Format(CommandList.CALLBACK_DATA_FORMAT_TRACK, Mbid, "Whiskey in the Jar")),
        });
    }

    [Fact]
    public void GetTracksInlineKeyboardMenu_Tracks_NumbersTracks()
    {
        // Act
        var result = InlineKeyboardHelper.GetTracksInlineKeyboardMenu(new[] { CreateTrack("One"), CreateTrack("Fuel") }, Mbid);

        // Assert
        Buttons(result).ShouldBe(new[]
        {
            ("1. One", string.Format(CommandList.CALLBACK_DATA_FORMAT_TRACK, Mbid, "One")),
            ("2. Fuel", string.Format(CommandList.CALLBACK_DATA_FORMAT_TRACK, Mbid, "Fuel")),
        });
    }

    [Fact]
    public void GetSetlistsInlineKeyboardMenu_Setlist_UsesArtistAndSetlistIds()
    {
        // Arrange
        var setlist = new MusicSetlist { Id = "4b48efd6", ArtistMBID = Mbid, ArtistName = "Metallica", EventDate = "31-10-2026", VenueName = "Sphere" };

        // Act
        var result = InlineKeyboardHelper.GetSetlistsInlineKeyboardMenu(new[] { setlist });

        // Assert
        Buttons(result).ShouldBe(new[]
        {
            (setlist.ToString(), string.Format(CommandList.CALLBACK_DATA_FORMAT_SETLIST, Mbid, "4b48efd6")),
        });
    }

    [Fact]
    public void GetArtistsInlineKeyboard_CounterGiven_NumbersFromCounter()
    {
        // Arrange
        var artist = A.Fake<MusicArtistBase>();
        A.CallTo(() => artist.Name).Returns("Metallica");
        A.CallTo(() => artist.ActiveYears).Returns("1981 - ");
        A.CallTo(() => artist.Area).Returns("United States");
        A.CallTo(() => artist.MBID).Returns(Mbid);

        // Act
        var result = InlineKeyboardHelper.GetArtistsInlineKeyboard(new[] { artist }, counter: 6);

        // Assert
        Buttons(result).ShouldBe(new[]
        {
            ("6. Metallica (1981 - ) [United States]", string.Format(CommandList.CALLBACK_DATA_FORMAT_ARTIST, Mbid)),
        });
    }

    [Fact]
    public void GetArtistInlineKeyboardMenu_LastFmAvailable_HasTopTracksButton()
    {
        // Act
        var result = InlineKeyboardHelper.GetArtistInlineKeyboardMenu(Mbid, isTopTracksAvailable: true);

        // Assert
        Buttons(result).Select(x => x.Text).ShouldBe(new[] { "🎓 Biography", "📝 Setlists", "📻 Top tracks" });
    }

    [Fact]
    public void GetArtistInlineKeyboardMenu_LastFmNotAvailable_NoTopTracksButton()
    {
        // Act
        var result = InlineKeyboardHelper.GetArtistInlineKeyboardMenu(Mbid, isTopTracksAvailable: false);

        // Assert
        Buttons(result).Select(x => x.Text).ShouldBe(new[] { "🎓 Biography", "📝 Setlists" });
    }

    [Theory]
    [InlineData(1, 0, true, new[] { "❌", "➡️" })]
    [InlineData(2, 0, true, new[] { "⬅️", "❌", "➡️" })]
    [InlineData(0, 5, true, new[] { "❌", "➡️" })]
    [InlineData(5, 5, true, new[] { "⬅️", "❌", "➡️" })]
    [InlineData(2, 0, false, new[] { "⬅️", "❌" })]
    public void WithNavigationButtons_Page_ShowsAvailableDirections(int page, int limit, bool forward, string[] expected)
    {
        // Act
        var result = new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>())
            .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SETLISTS, Mbid, page, limit, isForwardNavigationEnabled: forward);

        // Assert
        Buttons(result).Select(x => x.Text).ShouldBe(expected);
    }

    [Fact]
    public void WithDeleteButton_NoMessageIds_DeletesCurrentMessage()
    {
        // Act
        var result = new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>()).WithDeleteButton();

        // Assert
        Buttons(result).ShouldBe(new[] { ("❌", CommandList.CALLBACK_DATA_FORMAT_DELETE) });
    }

    [Fact]
    public void WithDeleteButton_MessageIds_PassesThemInCallback()
    {
        // Act
        var result = new InlineKeyboardMarkup(new List<InlineKeyboardButton[]>()).WithDeleteButton(10, 11);

        // Assert
        Buttons(result).ShouldBe(new[] { ("❌", $"{CommandList.COMMAND_DELETE} 10 11") });
    }

    private static MusicTrackBase CreateTrack(string name)
    {
        var track = A.Fake<MusicTrackBase>();
        A.CallTo(() => track.TrackName).Returns(name);
        return track;
    }

    private static (string Text, string CallbackData)[] Buttons(InlineKeyboardMarkup markup)
    {
        return markup.InlineKeyboard.SelectMany(row => row).Select(button => (button.Text, button.CallbackData!)).ToArray();
    }

    private static void AssertCallbackDataFits(InlineKeyboardMarkup markup)
    {
        foreach (var (_, callbackData) in Buttons(markup))
        {
            Encoding.UTF8.GetByteCount(callbackData).ShouldBeLessThanOrEqualTo(CallbackDataMaxBytes);
            callbackData.ShouldNotContain("�");
        }
    }
}
