using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper;

public static class MessageHelper
{
    public static async Task<Message> SendUnexpectedErrorAsync(ITelegramBotClient botClient, long chatId, string message = "")
    {
        string replyText = "Something goes wrong 😕! Please try another option or try again later.. " + message;

        InlineKeyboardMarkup inlineKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton();

        return await botClient.SendMessage(
            chatId: chatId,
            text: replyText,
            replyMarkup: inlineKeyboard);
    }
}
