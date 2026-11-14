using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper;

public static class InlineKeyboardHelper
{
    public static InlineKeyboardMarkup WithDeleteButton(this InlineKeyboardMarkup inlineKeyboardMarkup, params int[] messageIds)
    {
        var inlineKeyboardButtons = inlineKeyboardMarkup.InlineKeyboard;
        inlineKeyboardButtons = inlineKeyboardButtons.Append(new List<InlineKeyboardButton> { GetDeleteButton(messageIds) });
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    private static InlineKeyboardButton GetDeleteButton(params int[] messageIds)
    {
        if (messageIds != null && messageIds.Any())
        {
            string command = CommandList.COMMAND_DELETE + " " + string.Join(" ", messageIds.Select(x => x.ToString()));
            return InlineKeyboardButton.WithCallbackData("❌", command);
        }
        return InlineKeyboardButton.WithCallbackData("❌", string.Format(CommandList.CALLBACK_DATA_FORMAT_DELETE));
    }
}
