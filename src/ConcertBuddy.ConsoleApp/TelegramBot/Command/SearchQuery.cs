namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// What to search: the text of a message, or the name after "/search".
/// </summary>
public sealed record SearchQuery(string Text);
