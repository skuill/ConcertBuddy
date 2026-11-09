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
    private const string UnknownCommand = "unknown";

    private readonly ILogger<IBotHandlers> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;
    private readonly TimeProvider _timeProvider;

    public BotHandlers(ILogger<IBotHandlers> logger, IServiceProvider serviceProvider, ITelegramBotClient botClient, TimeProvider timeProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botClient = botClient;
        _timeProvider = timeProvider;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var startTimestamp = _timeProvider.GetTimestamp();
        var (commandName, createCommand) = FindCommand(update);
        var result = CommandResult.Found;

        try
        {
            if (createCommand != null)
            {
                var command = createCommand();
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

        // Only names and numbers here: this line is written for every update, so it must not hold what the user typed.
        _logger.LogInformation(
            "Handled update: {UpdateType} {Command:l} in {ChatType} chat: {Result} in {ElapsedMs} ms",
            update.Type,
            commandName,
            GetChat(update)?.Type,
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

        return message.GetSplitMessageText()[0] switch
        {
            CommandList.COMMAND_START => Route<UsageCommand>(CommandList.COMMAND_START, message),
            _ => Route<SearchMessageCommand>(CommandList.COMMAND_SEARCH, message),
        };
    }

    // Process Inline Keyboard callback data
    private (string Name, Func<IExecutableCommand>? Create) FindCallbackCommand(CallbackQuery callbackQuery)
    {
        return callbackQuery.GetSplitMessageText()[0] switch
        {
            CommandList.COMMAND_ARTIST => Route<ArtistCommand>(CommandList.COMMAND_ARTIST, callbackQuery),
            CommandList.COMMAND_SEARCH => Route<SearchCallbackCommand>(CommandList.COMMAND_SEARCH, callbackQuery),
            CommandList.COMMAND_BIOGRAPHY => Route<BiographyCommand>(CommandList.COMMAND_BIOGRAPHY, callbackQuery),
            CommandList.COMMAND_SETLISTS => Route<SetlistsCommand>(CommandList.COMMAND_SETLISTS, callbackQuery),
            CommandList.COMMAND_SETLIST => Route<SetlistCommand>(CommandList.COMMAND_SETLIST, callbackQuery),
            CommandList.COMMAND_TRACK => Route<TrackCommand>(CommandList.COMMAND_TRACK, callbackQuery),
            CommandList.COMMAND_LYRIC => Route<LyricCommand>(CommandList.COMMAND_LYRIC, callbackQuery),
            CommandList.COMMAND_DELETE => Route<DeleteCommand>(CommandList.COMMAND_DELETE, callbackQuery),
            CommandList.COMMAND_TOP => Route<TopCommand>(CommandList.COMMAND_TOP, callbackQuery),
            _ => Route<UsageCommand>(UnknownCommand, callbackQuery.Message!),
        };
    }

    private (string Name, Func<IExecutableCommand>? Create) Route<TCommand>(string name, object data) where TCommand : class, IExecutableCommand
    {
        return (name, () => ActivatorUtilities.CreateInstance<TCommand>(_serviceProvider, data));
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

    private static Chat? GetChat(Update update)
    {
        return update.Message?.Chat ?? update.EditedMessage?.Chat ?? update.CallbackQuery?.Message?.Chat;
    }
}
