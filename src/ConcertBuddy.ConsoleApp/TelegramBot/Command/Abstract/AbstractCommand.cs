using ConcertBuddy.ConsoleApp.Search;
using MusicSearcher;
using Telegram.Bot;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract
{
    public abstract class AbstractCommand<TResult, TData> : ICommand<TResult>
    {
        protected IMusicSearcherClient MusicSearcherClient { get; set; }

        protected ITelegramBotClient TelegramBotClient { get; set; }

        protected TData Data { get; set; }

        public AbstractCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, TData data)
        {
            MusicSearcherClient = musicSearcherClient;
            TelegramBotClient = telegramBotClient;
            Data = data;
        }

        public virtual Task<TResult> ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
