using ConcertBuddy.ConsoleApp.TelegramBot.Command;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler
{
    public class BotHandlers : IBotHandlers
    {
        private readonly ILogger<IBotHandlers> _logger;
        private readonly IMusicSearcherClient _musicSearcherClient;

        public BotHandlers(ILogger<IBotHandlers> logger, IMusicSearcherClient musicSearcherClient)
        {
            _logger = logger;
            _musicSearcherClient = musicSearcherClient;
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
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
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

        private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (message.Type != MessageType.Text)
                return;
            var action = message.GetSplitMessageText()[0] switch
            {
                $"{CommandList.COMMAND_START}" => new UsageCommand(_musicSearcherClient, botClient, message).ExecuteAsync(),
                _ => new SearchMessageCommand(_musicSearcherClient, botClient, message).ExecuteAsync()
            };
        }

        // Process Inline Keyboard callback data
        private async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var action = callbackQuery.GetSplitMessageText()[0] switch
            {
                $"{CommandList.COMMAND_ARTIST}" => new ArtistCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_SEARCH}" => new SearchCallbackCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_BIOGRAPHY}" => new BiographyCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_SETLISTS}" => new SetlistsCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_SETLIST}" => new SetlistCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_TRACK}" => new TrackCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_LYRIC}" => new LyricCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_DELETE}" => new DeleteCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                $"{CommandList.COMMAND_TOP}" => new TopCommand(_musicSearcherClient, botClient, callbackQuery).ExecuteAsync(),
                _ => new UsageCommand(_musicSearcherClient, botClient, callbackQuery.Message).ExecuteAsync()
            };
        }

        private Task BotOnChosenInlineResultReceived(ITelegramBotClient botClient, ChosenInlineResult chosenInlineResult)
        {
            _logger?.LogInformation($"Received inline result: {chosenInlineResult.ResultId}");
            return Task.CompletedTask;
        }

        private Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            _logger?.LogInformation($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}
