using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Command;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

public class BotHandlers : IBotHandlers
{
    private readonly ILogger<IBotHandlers> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BotHandlers(ILogger<IBotHandlers> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // Ignore the unavailability of Telegram servers when polling. 502 - Bad Gateway.
        if (exception is ApiRequestException apiRequestException && apiRequestException.ErrorCode == 502)
            return Task.CompletedTask;

        var ErrorMessage = exception switch
        {
            ApiRequestException requestException => $"Telegram API Error:\n[{requestException.ErrorCode}]\n{requestException.Message}",
            _ => exception.ToString()
        };

        _logger?.LogError(ErrorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            // UpdateType.InlineQuery:
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
            UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        if (message.Type != MessageType.Text)
            return;
        Task action = message.GetSplitMessageText()[0] switch
        {
            $"{CommandList.COMMAND_START}" => Create<UsageCommand>(message).ExecuteAsync(),
            _ => Create<SearchMessageCommand>(message).ExecuteAsync()
        };
        await action;
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        Task action = callbackQuery.GetSplitMessageText()[0] switch
        {
            $"{CommandList.COMMAND_ARTIST}" => Create<ArtistCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_SEARCH}" => Create<SearchCallbackCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_BIOGRAPHY}" => Create<BiographyCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_SETLISTS}" => Create<SetlistsCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_SETLIST}" => Create<SetlistCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_TRACK}" => Create<TrackCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_LYRIC}" => Create<LyricCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_DELETE}" => Create<DeleteCommand>(callbackQuery).ExecuteAsync(),
            $"{CommandList.COMMAND_TOP}" => Create<TopCommand>(callbackQuery).ExecuteAsync(),
            _ => Create<UsageCommand>(callbackQuery.Message!).ExecuteAsync()
        };
        await action;
    }

    private TCommand Create<TCommand>(object data) where TCommand : class
    {
        return ActivatorUtilities.CreateInstance<TCommand>(_serviceProvider, data);
    }

    private Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult)
    {
        _logger?.LogInformation($"Received inline result: {chosenInlineResult.ResultId}");
        return Task.CompletedTask;
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger?.LogInformation($"Unknown update type: {update.Type}");
        return Task.CompletedTask;
    }
}
