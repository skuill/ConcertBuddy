namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;

/// <summary>
/// What the update handler needs from any command: to run it and to know how it ended.
/// </summary>
public interface IExecutableCommand
{
    CommandResult Result { get; }

    Task RunAsync();
}
