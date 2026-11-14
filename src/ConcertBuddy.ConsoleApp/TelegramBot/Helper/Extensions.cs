using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper;

public static class Extensions
{
    private static readonly char MESSAGE_TEXT_DELIMETER = ' ';

    public static string[] GetSplitMessageText(this Message message)
    {
        return message.Text!.Trim().Split(MESSAGE_TEXT_DELIMETER);
    }

    public static string GetClearMessage(this Message message)
    {
        return message.Text!.Trim();
    }

    public static string[] GetSplitMessageText(this CallbackQuery callbackQuery)
    {
        return callbackQuery.Data!.Trim().Split(MESSAGE_TEXT_DELIMETER);
    }

    public static string[] GetParametersFromMessageText(this CallbackQuery callbackQuery, string command)
    {
        return callbackQuery.Data!.Trim().Replace(command, "").Trim().Split(MESSAGE_TEXT_DELIMETER);
    }
}
