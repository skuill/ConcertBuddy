using ConcertBuddy.ConsoleApp.TelegramBot;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Shouldly;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Helper;

public sealed class InlineKeyboardHelperTest
{
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

    private static (string Text, string CallbackData)[] Buttons(InlineKeyboardMarkup markup)
    {
        return markup.InlineKeyboard.SelectMany(row => row).Select(button => (button.Text, button.CallbackData!)).ToArray();
    }
}
