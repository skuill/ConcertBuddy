namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// One tap on a message: its screens replace the message only while no newer tap on it has started (#92).
/// </summary>
public sealed class ScreenEdit : IDisposable
{
    private readonly ScreenSender _sender;
    private readonly ScreenVersions _versions;
    private readonly long _chatId;
    private readonly int _messageId;
    private readonly long _version;

    internal ScreenEdit(ScreenSender sender, ScreenVersions versions, long chatId, int messageId)
    {
        _sender = sender;
        _versions = versions;
        _chatId = chatId;
        _messageId = messageId;
        _version = versions.Begin(chatId, messageId);
    }

    /// <summary>
    /// False when a newer tap on the message took over: the screen is dropped and the message keeps the newer one.
    /// </summary>
    public async Task<bool> EditAsync(Screen screen)
    {
        if (!_versions.IsLatest(_chatId, _messageId, _version))
        {
            return false;
        }

        await _sender.EditAsync(_chatId, _messageId, screen);
        return true;
    }

    public void Dispose()
    {
        _versions.End(_chatId, _messageId, _version);
    }
}
