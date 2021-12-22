﻿using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler
{
    public class BotHandlers : IBotHandlers
    {
        private readonly ILogger<IBotHandlers> _logger;
        private readonly ISearchHandler _searchHandler;

        public BotHandlers(ILogger<IBotHandlers> logger, ISearchHandler searchHandler)
        {
            _logger = logger;
            _searchHandler = searchHandler;
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(ErrorMessage);
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
            _logger.LogInformation($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;
            var action = message.GetSplitMessageText()[0] switch
            {
                $"{CommandList.COMMAND_START}" => new UsageCommand(_searchHandler, botClient, message).Execute(),
                _ => new SearchCommand(_searchHandler, botClient, message).Execute()
            };
            Message sentMessage = await action;
            _logger.LogInformation($"The message was sent with id: {sentMessage?.MessageId}");

        }

        // Process Inline Keyboard callback data
        private async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var action = callbackQuery.GetSplitMessageText()[0] switch
            {
                $"{CommandList.COMMAND_ARTIST}" => new ArtistCommand(_searchHandler, botClient, callbackQuery).Execute(),
                $"{CommandList.COMMAND_BIOGRAPHY}" => new BiographyCommand(_searchHandler, botClient, callbackQuery).Execute(),
                $"{CommandList.COMMAND_SETLISTS}" => new SetlistsCommand(_searchHandler, botClient, callbackQuery).Execute(),
                $"{CommandList.COMMAND_DELETE}" => new DeleteCommand(_searchHandler, botClient, callbackQuery).Execute(),
                _ => new UsageCommand(_searchHandler, botClient, callbackQuery.Message).Execute()
            };
        }

        private Task BotOnChosenInlineResultReceived(ITelegramBotClient botClient, ChosenInlineResult chosenInlineResult)
        {
            _logger.LogInformation($"Received inline result: {chosenInlineResult.ResultId}");
            return Task.CompletedTask;
        }

        private Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            _logger.LogInformation($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}
