namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

/// <summary>
/// Who the bot is, from getMe on start: the username makes the share links (t.me/&lt;username&gt;?start=...).
/// The test bot and the production bot have different usernames, so it is not in the code.
/// </summary>
public sealed class BotIdentity
{
    private volatile string? _username;

    public string? Username
    {
        get => _username;
        set => _username = value;
    }

    /// <summary>
    /// A link that opens the bot with the start parameter, null until the bot knows its username.
    /// </summary>
    public string? StartLink(string parameter)
    {
        return Username == null ? null : $"https://t.me/{Username}?start={parameter}";
    }
}
