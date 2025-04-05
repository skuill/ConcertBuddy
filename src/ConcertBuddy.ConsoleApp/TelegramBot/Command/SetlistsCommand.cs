using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class SetlistsCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_SETLISTS;

        private ILogger<SetlistsCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<SetlistsCommand>>();

        public SetlistsCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message?> ExecuteAsync()
        {
            _logger?.LogDebug($"Handle [{CurrentCommand}] command: [{Data.Data}]");

            if (Data == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Unexpected case. [Data] field is null.");
                return null;
            }
            if (Data!.Message == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Unexpected case. [Data.Message] field is null.");
            }

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CurrentCommand, out string errorMessage);
            if (!isValidQuery)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Error: {errorMessage}");
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            string replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CurrentCommand);
            if (!int.TryParse(parameters[0], out var page))
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't parse parameter: [{parameters[0]}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);

            }
            // ingore limit in parameters[1]. NOT USED BY LAST.FM
            var artistMBID = parameters[2];

            var setlists = await SearchHandler.SearchArtistSetlists(artistMBID, page);

            if (setlists == null || setlists.Setlist == null || setlists.Setlist.Count == 0)
            {
                replyText = $"Nothing found here 😕! Try another search or go back";

                await TelegramBotClient.AnswerCallbackQuery(
                    callbackQueryId: Data.Id,
                    text: $"{replyText}");

                if (page == SearchConstants.SEARCH_SETLISTS_PAGE_DEFAULT)
                {
                    var deleteKeyboard = InlineKeyboardMarkup.Empty()
                        .WithDeleteButton();
                    return await TelegramBotClient.SendMessage(
                        chatId: Data.Message.Chat.Id,
                        text: replyText,
                        replyMarkup: deleteKeyboard);
                }

                var navigationKeyboard = InlineKeyboardMarkup.Empty()
                    .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SETLISTS, artistMBID, page, isForwardNavigationEnabled: false);
                
                return await TelegramBotClient.SendMessage(
                    chatId: Data.Message.Chat.Id,
                    text: replyText,
                    replyMarkup: navigationKeyboard);
            }

            replyText = $"Found {setlists.Total} setlists 📝 .Please select a setlist:";

            bool isForwardNavigationEnabled = setlists.ItemsPerPage == setlists.Setlist.Count;
            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetSetlistsInlineKeyboardMenu(setlists.Setlist)
                .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SETLISTS, artistMBID, page, isForwardNavigationEnabled: isForwardNavigationEnabled);

            if (page != SearchConstants.SEARCH_SETLISTS_PAGE_DEFAULT)
                return await TelegramBotClient.EditMessageText(
                    chatId: Data.Message.Chat.Id,
                    messageId: Data.Message.MessageId,
                    text: replyText,
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html);


            return await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: replyText,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
        }
    }
}
