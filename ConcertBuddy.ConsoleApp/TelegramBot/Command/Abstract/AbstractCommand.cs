using ConcertBuddy.ConsoleApp.Search;
using Telegram.Bot;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract
{
    public class AbstractCommand<TResult, TData> : ICommand<TResult>
    {
        protected ISearchHandler SearchHandler { get; set; }

        protected ITelegramBotClient TelegramBotClient { get; set; }

        protected TData Data { get; set; }

        public AbstractCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, TData data)
        {
            SearchHandler = searchHandler;
            TelegramBotClient = telegramBotClient;
            Data = data;
        }

        public Task<TResult> Execute()
        {
            throw new NotImplementedException();
        }
    }
}
