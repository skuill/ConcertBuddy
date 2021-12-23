using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class DeleteCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<DeleteCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<DeleteCommand>>();

        public DeleteCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data) 
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            await TelegramBotClient.DeleteMessageAsync(Data.Message.Chat.Id, Data.Message.MessageId);
            return null;
        }
    }
}
