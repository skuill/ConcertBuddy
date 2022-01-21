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
    public class SetlistsCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<SetlistsCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<SetlistsCommand>>();

        public SetlistsCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data) 
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_SETLISTS}] command: [{Data.Data}]");

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_SETLISTS, out string errorMessage);
            if (!isValidQuery)
            {
                _logger.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            var replyText = "Please select a setlist:";
            
            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_SETLISTS);
            var page = int.Parse(parameters[0]);
            // ingore limit in parameters[1]. NOT USED BY LAST.FM
            var artistMBID = parameters[2];
            
            var setlists = await SearchHandler.SearchArtistSetlists(artistMBID, page);

            if (setlists == null || setlists.IsEmpty())
            {
                replyText = $"Nothing found there! Try another search or go back:";

                await TelegramBotClient.AnswerCallbackQueryAsync(
                    callbackQueryId: Data.Id,
                    text: $"{replyText}");

                if (page == SearchConstants.SEARCH_SETLISTS_PAGE_DEFAULT)
                {
                    return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());

                }

                var emptyKeyboard = InlineKeyboardMarkup.Empty()
                    .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SETLISTS, artistMBID, page);
                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: emptyKeyboard);
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetSetlistsInlineKeyboardMenu(setlists.Items)
                .WithNavigationButtons(CommandList.CALLBACK_DATA_FORMAT_SETLISTS, artistMBID, page);

            if (page != SearchConstants.SEARCH_SETLISTS_PAGE_DEFAULT)
                return await TelegramBotClient.EditMessageTextAsync(chatId: Data.Message.Chat.Id,
                                                           messageId: Data.Message.MessageId,
                                                           text: replyText,
                                                           replyMarkup: inlineKeyboard,
                                                           parseMode: ParseMode.Html);


            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard,
                                                       parseMode: ParseMode.Html);
        }
    }
}
