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

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_BIOGRAPHY}] command: [{Data.Data}]");

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_BIOGRAPHY, out string errorMessage);
            if (!isValidQuery)
            {
                _logger.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            string replyText = "Sorry, but the biography of this artist was not found ☹️";

            var artistMBID = Data.GetParameterFromMessageText(CommandList.COMMAND_BIOGRAPHY);
            
            var artist = await SearchHandler.SearchArtistByMBID(artistMBID);
            if (artist.Biography != null)
            {
                replyText = artist.Biography;
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton();

            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard,
                                                       parseMode: ParseMode.Html);
        }
    }
}
