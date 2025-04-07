using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using MusicSearcher;
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

        public SetlistsCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(musicSearcherClient, telegramBotClient, data)
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
            if (!int.TryParse(parameters[0], out var currentPage)
                || !int.TryParse(parameters[1], out var previousPage))
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't parse parameter: [{parameters[0]}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var artistMBID = parameters[2];

            var setlists = await MusicSearcherClient.SearchArtistSetlists(artistMBID, currentPage);

            if (setlists == null || setlists.Setlist == null || setlists.Setlist.Count == 0)
            {
                replyText = $"Nothing found here 😕! Try another search or go back";

                await TelegramBotClient.AnswerCallbackQuery(
                    callbackQueryId: Data.Id,
                    text: $"{replyText}");

                if (currentPage == SearchConstants.SEARCH_SETLISTS_PAGE_DEFAULT)
                {
                    var deleteKeyboard = InlineKeyboardMarkup.Empty()
                        .WithDeleteButton();
                    return await TelegramBotClient.SendMessage(
                        chatId: Data.Message.Chat.Id,
                        text: replyText,
                        replyMarkup: deleteKeyboard);
                }

                var navigationKeyboard = InlineKeyboardMarkup.Empty()
                    .WithNavigationButtons(
                        CommandList.CALLBACK_DATA_FORMAT_SETLISTS,
                        artistMBID,
                        currentPage,
                        isPreviousPageUsed: true,
                        isForwardNavigationEnabled: false);

                return await TelegramBotClient.SendMessage(
                    chatId: Data.Message.Chat.Id,
                    text: replyText,
                    replyMarkup: navigationKeyboard);
            }

            replyText = $"Found {setlists.Total} setlists 📝. Page: [{currentPage}/{setlists.TotalPages}]. Please select a setlist:";

            bool isForwardNavigationEnabled = setlists.ItemsPerPage == setlists.Setlist.Count;
            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetSetlistsInlineKeyboardMenu(setlists.Setlist)
                .WithNavigationButtons(
                    CommandList.CALLBACK_DATA_FORMAT_SETLISTS,
                    artistMBID, currentPage,
                    isPreviousPageUsed: true,
                    isForwardNavigationEnabled: isForwardNavigationEnabled);

            if (currentPage != previousPage)
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
