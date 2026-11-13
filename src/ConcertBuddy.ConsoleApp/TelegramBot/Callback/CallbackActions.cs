namespace ConcertBuddy.ConsoleApp.TelegramBot.Callback;

/// <summary>
/// Actions of the buttons, one or two letters to keep the callback data short. Data starting with "/"
/// belongs to the old commands that are not screens yet.
/// </summary>
public static class CallbackActions
{
    /// <summary>
    /// Artist search: offset|text.
    /// </summary>
    public const string Search = "s";

    /// <summary>
    /// Artist card: mbid|offset|text, the offset and text of the search lead back to its results (empty without a search).
    /// </summary>
    public const string Artist = "a";
}
