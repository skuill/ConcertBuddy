using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

/// <summary>
/// A screen as readable text, so a test compares the whole screen at once: the text, every button with its data,
/// style and state, the preview.
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
        var text = button.CallbackData == null ? button.Text : $"{button.Text} → {button.CallbackData}";
        if (button.Style != null)
        {
            text += $" [{button.Style}]";
        }
        if (button.Disabled != null)
        {
            text += " [disabled]";
        }
        return text;
    }
}
