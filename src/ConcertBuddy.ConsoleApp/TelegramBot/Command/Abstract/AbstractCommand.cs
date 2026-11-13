using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;

public abstract class AbstractCommand<TResult, TData> : ICommand<TResult>, IExecutableCommand
{
    protected IMusicSearcherClient MusicSearcherClient { get; set; }

    protected ITelegramBotClient TelegramBotClient { get; set; }

    protected TData Data { get; set; }

    public CommandResult Result { get; protected set; } = CommandResult.Found;

    public bool AnsweredCallback { get; protected set; }

    public AbstractCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, TData data)
    {
        MusicSearcherClient = musicSearcherClient;
        TelegramBotClient = telegramBotClient;
        Data = data;
    }

    public Task RunAsync() => ExecuteAsync();

    public virtual Task<TResult> ExecuteAsync()
    {
        throw new NotImplementedException();
    }

    protected Task<Message> SendNotFoundAsync(long chatId, string message = "")
    {
        Result = CommandResult.NotFound;
        return MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, chatId, message);
    }
}
