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

        public async Task<Message> Execute()
        {
            var isValidQuery = await CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_SETLISTS);
            if (!isValidQuery)
                return null;

            var replyText = "Please select a setlist:";

            var mbid = Data.GetParameterFromMessageText(CommandList.COMMAND_SETLISTS);
            var setlists = await SearchHandler.SearchArtistSetlists(mbid);

            if (setlists == null || setlists.IsEmpty())
            {
                replyText = $"Can't find any setlist for artist MBID {mbid}";

                await TelegramBotClient.AnswerCallbackQueryAsync(
                    callbackQueryId: Data.Id,
                    text: $"{replyText}");

                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetSetlistsInlineKeyboardMenu(setlists.Items)
                .WithDeleteButton();

            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard,
                                                       parseMode: ParseMode.Html);
        }
    }
}
