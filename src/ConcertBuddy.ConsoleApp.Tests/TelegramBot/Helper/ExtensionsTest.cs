using ConcertBuddy.ConsoleApp.Model;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Shouldly;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Helper;

public class ExtensionsTest
{
    private const string Command = "/track";
    private const string Text = "  /track 827e1b52 Nothing Else Matters  ";

    [Fact]
    public void GetSplitMessageText_Message_SplitsTrimmedTextBySpace()
    {
        // Act
        var result = new Message { Text = Text }.GetSplitMessageText();

        // Assert
        result.ShouldBe(new[] { "/track", "827e1b52", "Nothing", "Else", "Matters" });
    }

    [Fact]
    public void GetClearMessage_Message_ReturnsTrimmedText()
    {
        // Act
        var result = new Message { Text = Text }.GetClearMessage();

        // Assert
        result.ShouldBe("/track 827e1b52 Nothing Else Matters");
    }

    [Fact]
    public void GetParameterFromMessageText_Message_ReturnsTextWithoutCommand()
    {
        // Act
        var result = new Message { Text = Text }.GetParameterFromMessageText(Command);

        // Assert
        result.ShouldBe("827e1b52 Nothing Else Matters");
    }

    [Fact]
    public void GetParametersFromMessageText_Message_SplitsTextWithoutCommand()
    {
        // Act
        var result = new Message { Text = Text }.GetParametersFromMessageText(Command);

        // Assert
        result.ShouldBe(new[] { "827e1b52", "Nothing", "Else", "Matters" });
    }

    [Fact]
    public void GetSplitMessageText_CallbackQuery_SplitsTrimmedDataBySpace()
    {
        // Act
        var result = new CallbackQuery { Data = Text }.GetSplitMessageText();

        // Assert
        result.ShouldBe(new[] { "/track", "827e1b52", "Nothing", "Else", "Matters" });
    }

    [Fact]
    public void GetParameterFromMessageText_CallbackQuery_ReturnsDataWithoutCommand()
    {
        // Act
        var result = new CallbackQuery { Data = Text }.GetParameterFromMessageText(Command);

        // Assert
        result.ShouldBe("827e1b52 Nothing Else Matters");
    }

    [Fact]
    public void GetParametersFromMessageText_CallbackQuery_SplitsDataWithoutCommand()
    {
        // Act
        var result = new CallbackQuery { Data = Text }.GetParametersFromMessageText(Command);

        // Assert
        result.ShouldBe(new[] { "827e1b52", "Nothing", "Else", "Matters" });
    }

    [Fact]
    public void ConvertToString_EnumValue_ReturnsLowerCaseName()
    {
        // Act
        var result = SearchType.Track.ConvertToString();

        // Assert
        result.ShouldBe("track");
    }
}
