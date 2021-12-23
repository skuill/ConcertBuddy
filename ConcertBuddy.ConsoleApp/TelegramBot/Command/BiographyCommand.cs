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
    public class BiographyCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<BiographyCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<BiographyCommand>>();

        public BiographyCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data) 
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            var isValidQuery = await CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_BIOGRAPHY);
            if (!isValidQuery)
                return null;

            string replyText = "Sorry, but the biography of this artist was not found ☹️";

            var mbid = Data.GetParameterFromMessageText(CommandList.COMMAND_BIOGRAPHY);
            var artist = await SearchHandler.SearchArtistByMBID(mbid);
            if (artist.Biography != null)
            {
                replyText = artist.Biography;
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistInlineKeyboardMenu(mbid);

            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard,
                                                       parseMode: ParseMode.Html);
        }
    }
}
