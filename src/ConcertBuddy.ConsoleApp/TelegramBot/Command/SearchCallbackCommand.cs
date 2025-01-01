using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class SearchCallbackCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_SEARCH;

        private ILogger<SearchCallbackCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<SearchCallbackCommand>>();

        public SearchCallbackCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message?> ExecuteAsync()
        {
            _logger?.LogDebug($"Handle [{CurrentCommand}] callback command: [{Data.Data}]");

            if (Data == null)
            {
                _logger?.LogError($"Unexpected case. [Data] field is null. Command: [{CurrentCommand}]");
                return null;
            }
            if (Data!.Message == null)
            {
                _logger?.LogError($"Unexpected case. [Data.Message] field is null. Command: [{CurrentCommand}]");
            }

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CurrentCommand, out string errorMessage);
            if (!isValidQuery)
            {
                _logger?.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            _logger?.LogDebug($"Handle search command: [{Data.Data}]");

            string replyText = string.Empty;
            string artistName = string.Empty;
            int offset = 0;
            int limit = 5;

            var splitMessage = Data.GetSplitMessageText();

            if (splitMessage.Count() == 1)
            {
                replyText = $"Please pass artist's name as a parameter. For example: [{CurrentCommand} The Beatles]";

                return await TelegramBotClient.SendMessage(
                    chatId: Data.Message.Chat.Id,
                    text: replyText,
                    replyMarkup: new ReplyKeyboardRemove());
            }

            var parameters = Data.GetParametersFromMessageText(CurrentCommand);
            offset = int.Parse(parameters[0]);
            limit = int.Parse(parameters[1]);
            artistName = String.Join(' ', parameters.Skip(2));

            var artists = await SearchHandler.SearchArtistsByName(artistName, limit, offset);

            if ((artists == null || !artists.Any()) && offset == 0)
            {
                if (offset == SearchConstants.SEARCH_ARTISTS_OFFSET_DEFAULT)
                {
                    _logger?.LogError($"Can't find artist [{artistName}]");
                    return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
                }

                InlineKeyboardMarkup navigationKeyboard = InlineKeyboardMarkup.Empty()
                    .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SEARCH, artistName, offset, limit);

                replyText = "Nothing found here 😕! Try another search or go back";
                return await TelegramBotClient.EditMessageText(
                    chatId: Data.Message.Chat.Id,
                    messageId: Data.Message.MessageId,
                    text: replyText,
                    replyMarkup: navigationKeyboard);
            }

            bool isForwardNavigationEnabled = artists!.Count() == limit;
            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistsInlineKeyboard(artists!, offset + 1)
                .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SEARCH, artistName, offset, limit, isForwardNavigationEnabled);

            replyText = "Choose the correct artist 💭:";

            return await TelegramBotClient.EditMessageText(
                chatId: Data.Message.Chat.Id,
                messageId: Data.Message.MessageId,
                text: replyText,
                replyMarkup: inlineKeyboard);
        }
    }
}
