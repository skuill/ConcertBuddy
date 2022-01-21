using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Validation
{
    public static class CallbackQueryValidation
    {
        public static bool Validate(ITelegramBotClient botClient, CallbackQuery callbackQuery, string command, out string errorMessage)
        {
            var splitMessage = callbackQuery.GetSplitMessageText();

            errorMessage = string.Empty;

            if (splitMessage.Count() == 1)
            {
                errorMessage = $"Please pass any parameters for command {command}!";
                return false;
            }

            if (!splitMessage.Contains(command))
            {
                errorMessage = $"Wrong command handler! The correct one should be {command}.";
                return false;
            }

            return true;
        }
    }
}
