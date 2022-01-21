using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper
{
    public static class MessageHelper
    {
        public static async Task SendAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, string text)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: text);

            await botClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: text);
        }
    }
}
