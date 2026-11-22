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
    private readonly ScreenVersions _versions;
    private readonly TimeProvider _timeProvider;

    public ScreenSender(ITelegramBotClient botClient, ScreenVersions versions, TimeProvider timeProvider)
    {
        _botClient = botClient;
        _versions = versions;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// A tap on the message starts: from now on an older tap on it no longer changes its screen (#92).
    /// </summary>
    public ScreenEdit BeginEdit(long chatId, int messageId)
    {
        return new ScreenEdit(this, _versions, _timeProvider, chatId, messageId);
    }

    public Task<Message> SendAsync(long chatId, Screen screen)
    {
        return screen.IsRich
            ? _botClient.SendRichMessage(chatId, Rich(screen), replyMarkup: screen.Keyboard)
            : _botClient.SendMessage(chatId, screen.Html, ParseMode.Html, replyMarkup: screen.Keyboard, linkPreviewOptions: Preview(screen));
    }

    public async Task EditAsync(long chatId, int messageId, Screen screen)
    {
        try
        {
            // A plain message can become a rich one and back: the artist card and its setlists share one message.
            await (screen.IsRich
                ? _botClient.EditMessageText(chatId, messageId, null!, replyMarkup: screen.Keyboard, richMessage: Rich(screen))
                : _botClient.EditMessageText(chatId, messageId, screen.Html, ParseMode.Html, screen.Keyboard, Preview(screen)));
        }
        catch (ApiRequestException exception) when (exception.Message.Contains("message is not modified"))
        {
            // The same screen again, for example a second tap on "Try again": nothing to change.
        }
    }

    private static InputRichMessage Rich(Screen screen)
    {
        return new InputRichMessage { Html = screen.Html, Media = screen.Media?.ToArray() };
    }

    private static LinkPreviewOptions Preview(Screen screen)
    {
        return screen.PreviewUrl == null
            ? new LinkPreviewOptions { IsDisabled = true }
            : new LinkPreviewOptions { Url = screen.PreviewUrl, ShowAboveText = true, PreferLargeMedia = true };
    }
}
