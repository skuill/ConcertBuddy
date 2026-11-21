namespace ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;

public enum CommandResult
{
    Found,
    NotFound,
    Error,

    /// <summary>
    /// A music service did not answer, the user got "Try again".
    /// </summary>
    Unavailable,

    /// <summary>
    /// A button of an older bot run, the user got "This button is outdated".
    /// </summary>
    Outdated,

    /// <summary>
    /// A newer tap on the same message ended first: this screen was dropped, the message keeps the newer one (#92).
    /// </summary>
    Superseded,
}
