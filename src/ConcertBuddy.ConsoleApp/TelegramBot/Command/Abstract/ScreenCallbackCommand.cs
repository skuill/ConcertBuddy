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

    /// <summary>
    /// A short toast while the screen is built, for slow screens; null for none.
    /// </summary>
    protected virtual string? AnswerText => null;

    /// <summary>
    /// "⏳ Loading…" when the screen takes more than a second (#94); off for a message whose content must stay, like a track
    /// with its player.
    /// </summary>
    protected virtual bool ShowsLoading => true;

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

        // Before anything else: a newer tap on this message from now on keeps its screen over this one (#92).
        using var edit = _sender.BeginEdit(Data.Message.Chat.Id, Data.Message.MessageId);

        // At once: MusicBrainz may take several seconds, the button must not spin all that time.
        await TelegramBotClient.AnswerCallbackQuery(Data.Id, AnswerText);
        AnsweredCallback = true;

        ScreenResult result;
        if (Waiting != null)
        {
            await edit.EditAsync(Waiting);
            result = await build();
        }
        else
        {
            result = ShowsLoading ? await edit.BuildWithLoadingAsync(build) : await build();
        }
        Result = await edit.EditAsync(result.Screen) ? result.Result : CommandResult.Superseded;
        return Data.Message;
    }
}
