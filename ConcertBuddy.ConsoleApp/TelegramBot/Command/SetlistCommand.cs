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
    public class SetlistCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<SetlistCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<SetlistCommand>>();

        public SetlistCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data) : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            return null;
        }
    }
}
