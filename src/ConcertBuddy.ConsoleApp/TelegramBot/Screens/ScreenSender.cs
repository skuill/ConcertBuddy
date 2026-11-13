using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// Sends a screen as a new message or puts it into an existing one.
/// </summary>
public sealed class ScreenSender
{
    private readonly ITelegramBotClient _botClient;

    public ScreenSender(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public Task<Message> SendAsync(long chatId, Screen screen)
    {
        return _botClient.SendMessage(chatId, screen.Html, ParseMode.Html, replyMarkup: screen.Keyboard, linkPreviewOptions: Preview(screen));
    }

    public async Task EditAsync(long chatId, int messageId, Screen screen)
    {
        try
        {
            await _botClient.EditMessageText(chatId, messageId, screen.Html, ParseMode.Html, screen.Keyboard, Preview(screen));
        }
        catch (ApiRequestException exception) when (exception.Message.Contains("message is not modified"))
        {
            // The same screen again, for example a second tap on "Try again": nothing to change.
        }
    }

    private static LinkPreviewOptions Preview(Screen screen)
    {
        return screen.PreviewUrl == null
            ? new LinkPreviewOptions { IsDisabled = true }
            : new LinkPreviewOptions { Url = screen.PreviewUrl, ShowAboveText = true, PreferLargeMedia = true };
    }
}
