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
    private readonly TimeProvider _timeProvider;

    internal ScreenEdit(ScreenSender sender, ScreenVersions versions, TimeProvider timeProvider, long chatId, int messageId)
    {
        _sender = sender;
        _versions = versions;
        _timeProvider = timeProvider;
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

    /// <summary>
    /// How long a screen may take before the message shows <see cref="CommonScreens.Loading"/> (#94).
    /// </summary>
    public static readonly TimeSpan LoadingDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Builds a screen and shows the loading screen meanwhile when it is not built within <see cref="LoadingDelay"/>, so a
    /// fast screen does not blink. The delay starts before the build, so it counts all of it.
    /// </summary>
    public async Task<T> BuildWithLoadingAsync<T>(Func<Task<T>> build)
    {
        using var stop = new CancellationTokenSource();
        var delay = Task.Delay(LoadingDelay, _timeProvider, stop.Token);
        var building = build();
        if (await Task.WhenAny(building, delay) == delay)
        {
            await EditAsync(CommonScreens.Loading);
        }
        stop.Cancel();
        return await building;
    }

    public void Dispose()
    {
        _versions.End(_chatId, _messageId, _version);
    }
}
