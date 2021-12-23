using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Validation
{
    public static class CallbackQueryValidation
    {
        public static async Task<bool> Validate(ITelegramBotClient botClient, CallbackQuery callbackQuery, string command = "/SOME_COMMAND")
        {
            var splitMessage = callbackQuery.GetSplitMessageText();

            if (splitMessage.Count() == 1)
            {
                string replyText = $"Please pass any parameters for command {command}!";

                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: replyText);

                await botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: replyText);

                return false;
            }
            return true;
        }
    }
}
