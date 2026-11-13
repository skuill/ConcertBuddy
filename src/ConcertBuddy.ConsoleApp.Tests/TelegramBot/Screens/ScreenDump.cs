using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

/// <summary>
/// A screen as readable text, so a test compares the whole screen at once: the text, every button with its data
/// and style, the preview.
/// </summary>
internal static class ScreenDump
{
    public static string Of(Screen screen)
    {
        var rows = screen.Keyboard.InlineKeyboard.Select(row => string.Join(" | ", row.Select(Button)));
        return string.Join("\n", new[] { screen.Html, "---" }.Concat(rows).Append($"preview: {screen.PreviewUrl ?? "none"}"));
    }

    private static string Button(InlineKeyboardButton button)
    {
        return button.Style == null ? $"{button.Text} → {button.CallbackData}" : $"{button.Text} → {button.CallbackData} [{button.Style}]";
    }
}
