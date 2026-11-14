namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

public enum InfoPage
{
    Start,
    Help,
    About,

    /// <summary>
    /// "/search" without a name.
    /// </summary>
    SearchHint,
}
