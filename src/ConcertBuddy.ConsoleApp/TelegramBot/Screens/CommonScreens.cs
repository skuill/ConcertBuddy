using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

public static class CommonScreens
{
    public const string OutdatedButton = "This button is outdated. Send the artist name again.";

    /// <summary>
    /// A service did not answer: it is not the user's query, so no tips, just "Try again" with the same button data.
    /// </summary>
    public static ScreenResult Unavailable(string service, string retryCallbackData)
    {
        var html = $"⚠️ {ScreenText.Html(service)} is busy or down right now. It is not your query: try again in a minute.";
        var tryAgain = InlineKeyboardButton.WithCallbackData("🔄 Try again", retryCallbackData);
        tryAgain.Style = KeyboardButtonStyle.Primary;
        return new ScreenResult(new Screen(html, new InlineKeyboardMarkup(new[] { tryAgain, DeleteButton() })), CommandResult.Unavailable);
    }

    /// <summary>
    /// Deletes the message (the old "/delete" command); the menu takes its place in a later step of #48.
    /// </summary>
    public static InlineKeyboardButton DeleteButton()
    {
        return InlineKeyboardButton.WithCallbackData("❌", CommandList.CALLBACK_DATA_FORMAT_DELETE);
    }
}
