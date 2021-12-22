namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract
{
    public interface ICommand<TResult>
    {
        Task<TResult> Execute();
    }
}
