using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class LyricCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<LyricCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<LyricCommand>>();

        public LyricCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            return null;
        }
    }
}
