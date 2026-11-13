using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// A built screen and how the search behind it ended, for the log line of the update.
/// </summary>
public sealed record ScreenResult(Screen Screen, CommandResult Result);
