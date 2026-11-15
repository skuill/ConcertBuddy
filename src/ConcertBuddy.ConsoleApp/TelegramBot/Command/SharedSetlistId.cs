namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// The setlist id of a share link (t.me/&lt;bot&gt;?start=s_&lt;id&gt;).
/// </summary>
public sealed record SharedSetlistId(string Id);
