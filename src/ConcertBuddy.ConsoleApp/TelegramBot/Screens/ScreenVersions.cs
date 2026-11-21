using System.Collections.Concurrent;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// The latest tap on a message wins (#92). Updates run in parallel, and a tap that waits for MusicBrainz retries can end
/// seconds after a newer tap on the same message: every tap takes the next version of its message, and only the latest
/// version may still replace the screen. A message is kept here only while a tap on it is in progress.
/// </summary>
public sealed class ScreenVersions
{
    private readonly ConcurrentDictionary<(long ChatId, int MessageId), long> _latest = new();
    private long _lastVersion;

    /// <summary>
    /// Messages with a tap in progress.
    /// </summary>
    internal int Count => _latest.Count;

    internal long Begin(long chatId, int messageId)
    {
        var version = Interlocked.Increment(ref _lastVersion);
        _latest[(chatId, messageId)] = version;
        return version;
    }

    /// <summary>
    /// False also when the message is gone from here: a newer tap started and ended in the meantime.
    /// </summary>
    internal bool IsLatest(long chatId, int messageId, long version)
    {
        return _latest.TryGetValue((chatId, messageId), out var latest) && latest == version;
    }

    internal void End(long chatId, int messageId, long version)
    {
        // Only the latest tap removes the message: an older one ends while the newer one still works.
        _latest.TryRemove(new KeyValuePair<(long, int), long>((chatId, messageId), version));
    }
}
