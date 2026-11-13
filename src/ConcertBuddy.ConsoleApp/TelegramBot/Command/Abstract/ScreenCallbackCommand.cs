using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;

/// <summary>
/// A button that replaces the screen of its own message (#48).
/// </summary>
public abstract class ScreenCallbackCommand : AbstractCommand<Message?, CallbackQuery>
{
    private readonly ScreenSender _sender;

    protected ScreenCallbackCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _sender = sender;
        Payload = payload;
    }

    protected CallbackPayload Payload { get; }

    protected abstract int ArgumentCount { get; }

    /// <summary>
    /// Null when the arguments are not what this button had: the button is from an older bot.
    /// </summary>
    protected abstract Func<Task<ScreenResult>>? Prepare(string[] arguments);

    /// <summary>
    /// The screen to show while the real one is built, or null to keep the current one.
    /// </summary>
    protected virtual Screen? Waiting => null;

    public override async Task<Message?> ExecuteAsync()
    {
        var arguments = Payload.Split(ArgumentCount);
        var build = arguments == null ? null : Prepare(arguments);
        if (build == null || Data.Message == null)
        {
            await OutdatedCommand.AnswerAsync(TelegramBotClient, Data);
            Result = CommandResult.Outdated;
            AnsweredCallback = true;
            return null;
        }

        // At once: MusicBrainz may take several seconds, the button must not spin all that time.
        await TelegramBotClient.AnswerCallbackQuery(Data.Id);
        AnsweredCallback = true;

        var chatId = Data.Message.Chat.Id;
        var messageId = Data.Message.MessageId;
        if (Waiting != null)
        {
            await _sender.EditAsync(chatId, messageId, Waiting);
        }

        var result = await build();
        Result = result.Result;
        await _sender.EditAsync(chatId, messageId, result.Screen);
        return Data.Message;
    }
}
