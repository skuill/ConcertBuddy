using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

public class BotHandlers : IBotHandlers
{
    private const string NoCommand = "none";
    private const string OutdatedCommandName = "outdated";
    private const string UnknownCommandName = "unknown";

    private readonly ILogger<IBotHandlers> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;
    private readonly TimeProvider _timeProvider;
    private readonly UserKeys _userKeys;
    private readonly CallbackCodec _callbackCodec;

    public BotHandlers(ILogger<IBotHandlers> logger, IServiceProvider serviceProvider, ITelegramBotClient botClient, TimeProvider timeProvider, UserKeys userKeys, CallbackCodec callbackCodec)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botClient = botClient;
        _timeProvider = timeProvider;
        _userKeys = userKeys;
        _callbackCodec = callbackCodec;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var startTimestamp = _timeProvider.GetTimestamp();
        var (commandName, createCommand) = FindCommand(update);
        var result = CommandResult.Found;
        IExecutableCommand? command = null;

        try
        {
            if (createCommand != null)
            {
                command = createCommand();
                await command.RunAsync();
                result = command.Result;
            }
        }
        catch (Exception exception)
        {
            result = CommandResult.Error;
            _logger.LogError(exception, "Failed to handle update {UpdateId} of type {UpdateType}, command {Command:l}", update.Id, update.Type, commandName);
            await TrySendErrorAsync(update);
        }

        // Every button press gets an answer, otherwise it keeps spinning in the app.
        if (update.CallbackQuery != null && command?.AnsweredCallback != true)
        {
            await TryAnswerCallbackAsync(update.CallbackQuery);
        }

        // Only names and numbers here: this line is written for every update, so it must not hold what the user typed.
        _logger.LogInformation(
            "Handled update: {UpdateType} {Command:l} in {ChatType} chat from user {UserKey:l}: {Result} in {ElapsedMs} ms",
            update.Type,
            commandName,
            GetChat(update)?.Type,
            _userKeys.Get(GetUserId(update)),
            result,
            (long)_timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds);
    }

    private (string Name, Func<IExecutableCommand>? Create) FindCommand(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => FindMessageCommand(update.Message!),
            UpdateType.EditedMessage => FindMessageCommand(update.EditedMessage!),
            UpdateType.CallbackQuery => FindCallbackCommand(update.CallbackQuery!),
            _ => (NoCommand, null),
        };
    }

    private (string Name, Func<IExecutableCommand>? Create) FindMessageCommand(Message message)
    {
        if (message.Type != MessageType.Text)
        {
            return (NoCommand, null);
        }

        var text = message.Text!.Trim();
        if (!text.StartsWith('/'))
        {
            return Route<SearchMessageCommand>(CommandList.COMMAND_SEARCH, message, new SearchQuery(text));
        }

        // "/search@ConcertBuddyBot Metallica" in groups, "/search Metallica" from the command menu.
        var parts = text.Split(' ', 2);
        var command = parts[0].Split('@')[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        return command switch
        {
            CommandList.COMMAND_START => Route<InfoCommand>(CommandList.COMMAND_START, message, InfoPage.Start),
            CommandList.COMMAND_HELP => Route<InfoCommand>(CommandList.COMMAND_HELP, message, InfoPage.Help),
            CommandList.COMMAND_ABOUT => Route<InfoCommand>(CommandList.COMMAND_ABOUT, message, InfoPage.About),
            CommandList.COMMAND_SEARCH when argument.Length == 0 => Route<InfoCommand>(CommandList.COMMAND_SEARCH, message, InfoPage.SearchHint),
            CommandList.COMMAND_SEARCH => Route<SearchMessageCommand>(CommandList.COMMAND_SEARCH, message, new SearchQuery(argument)),
            // An unknown command gets the help, not a search for "/whatever".
            _ => Route<InfoCommand>(UnknownCommandName, message, InfoPage.Help),
        };
    }

    private (string Name, Func<IExecutableCommand>? Create) FindCallbackCommand(CallbackQuery callbackQuery)
    {
        if (string.IsNullOrEmpty(callbackQuery.Data))
        {
            return Route<OutdatedCommand>(OutdatedCommandName, callbackQuery);
        }

        // The old commands, until they become screens too (#48). Buttons of the commands that are screens now are outdated.
        if (callbackQuery.Data.StartsWith('/'))
        {
            return FindOldCallbackCommand(callbackQuery);
        }

        if (!_callbackCodec.TryDecode(callbackQuery.Data, out var payload))
        {
            return Route<OutdatedCommand>(OutdatedCommandName, callbackQuery);
        }

        return payload.Action switch
        {
            CallbackActions.Search or CallbackActions.NewSearch => Route<SearchCallbackCommand>(CommandList.COMMAND_SEARCH, callbackQuery, payload),
            CallbackActions.Artist => Route<ArtistCommand>(CommandList.COMMAND_ARTIST, callbackQuery, payload),
            CallbackActions.Biography => Route<BiographyCommand>(CommandList.COMMAND_BIOGRAPHY, callbackQuery, payload),
            CallbackActions.Setlists => Route<SetlistsCommand>(CommandList.COMMAND_SETLISTS, callbackQuery, payload),
            CallbackActions.Setlist => Route<SetlistCommand>(CommandList.COMMAND_SETLIST, callbackQuery, payload),
            CallbackActions.TopTracks => Route<TopTracksCommand>(CommandList.COMMAND_TOP, callbackQuery, payload),
            CallbackActions.Play or CallbackActions.PlayAgain => Route<PlayCommand>(CommandList.COMMAND_TRACK, callbackQuery, payload),
            CallbackActions.ShowLyrics or CallbackActions.HideLyrics => Route<LyricsCommand>(CommandList.COMMAND_LYRIC, callbackQuery, payload),
            CallbackActions.Menu => Route<InfoCallbackCommand>(CommandList.COMMAND_START, callbackQuery, payload),
            CallbackActions.Help => Route<InfoCallbackCommand>(CommandList.COMMAND_HELP, callbackQuery, payload),
            CallbackActions.About => Route<InfoCallbackCommand>(CommandList.COMMAND_ABOUT, callbackQuery, payload),
            _ => Route<OutdatedCommand>(OutdatedCommandName, callbackQuery),
        };
    }

    private (string Name, Func<IExecutableCommand>? Create) FindOldCallbackCommand(CallbackQuery callbackQuery)
    {
        return callbackQuery.GetSplitMessageText()[0] switch
        {
            CommandList.COMMAND_DELETE => Route<DeleteCommand>(CommandList.COMMAND_DELETE, callbackQuery),
            _ => Route<OutdatedCommand>(OutdatedCommandName, callbackQuery),
        };
    }

    private (string Name, Func<IExecutableCommand>? Create) Route<TCommand>(string name, params object[] data) where TCommand : class, IExecutableCommand
    {
        return (name, () => ActivatorUtilities.CreateInstance<TCommand>(_serviceProvider, data));
    }

    private async Task TryAnswerCallbackAsync(CallbackQuery callbackQuery)
    {
        try
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id);
        }
        catch (Exception exception)
        {
            // An old command answered it already, or it is too old: the user sees nothing of this.
            _logger.LogDebug(exception, "Can't answer the callback query {CallbackQueryId}", callbackQuery.Id);
        }
    }

    private async Task TrySendErrorAsync(Update update)
    {
        var chat = GetChat(update);
        if (chat == null)
        {
            return;
        }

        try
        {
            await MessageHelper.SendUnexpectedErrorAsync(_botClient, chat.Id);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Can't tell the user about the failed update {UpdateId}", update.Id);
        }
    }

    private static long? GetUserId(Update update)
    {
        return (update.Message?.From ?? update.EditedMessage?.From ?? update.CallbackQuery?.From)?.Id;
    }

    private static Chat? GetChat(Update update)
    {
        return update.Message?.Chat ?? update.EditedMessage?.Chat ?? update.CallbackQuery?.Message?.Chat;
    }
}
