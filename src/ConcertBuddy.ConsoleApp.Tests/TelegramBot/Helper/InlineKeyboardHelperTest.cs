using ConcertBuddy.ConsoleApp.TelegramBot;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using FakeItEasy;
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
    public void GetTrackCallbackData_LongName_FitsTelegramLimit(string name)
    {
        // Act
        var result = InlineKeyboardHelper.GetTrackCallbackData(Mbid, name);

        // Assert
        Encoding.UTF8.GetByteCount(result).ShouldBeLessThanOrEqualTo(64);
        result.ShouldStartWith($"/track {Mbid} ");
    }

    [Fact]
    public void GetTrackCallbackData_ShortName_WholeName()
    {
        InlineKeyboardHelper.GetTrackCallbackData(Mbid, "One").ShouldBe($"/track {Mbid} One");
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
