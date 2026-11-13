namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;

/// <summary>
/// What the update handler needs from any command: to run it and to know how it ended.
/// </summary>
public interface IExecutableCommand
{
    CommandResult Result { get; }

    /// <summary>
    /// True when the command answered the callback query itself; otherwise the update handler answers it.
    /// </summary>
    bool AnsweredCallback { get; }

    Task RunAsync();
}
