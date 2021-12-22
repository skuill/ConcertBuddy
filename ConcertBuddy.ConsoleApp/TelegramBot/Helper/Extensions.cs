using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot
{
    public static class Extensions
    {
        private static readonly char MESSAGE_TEXT_DELIMETER = ' ';

        public static string[] GetSplitMessageText(this Message message)
        {
            return message.Text!.Trim().Split(MESSAGE_TEXT_DELIMETER);
        }

        public static string GetParameterFromMessageText(this Message message, string command)
        {
            return message.Text!.Trim().Replace(command, "").Trim();
        }

        public static string[] GetSplitMessageText(this CallbackQuery callbackQuery)
        {
            return callbackQuery.Data!.Trim().Split(MESSAGE_TEXT_DELIMETER);
        }

        public static string GetParameterFromMessageText(this CallbackQuery callbackQuery, string command)
        {
            return callbackQuery.Data!.Trim().Replace(command, "").Trim();
        }
    }
}
