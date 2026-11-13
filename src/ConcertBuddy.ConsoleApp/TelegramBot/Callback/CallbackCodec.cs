using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Callback;

/// <summary>
/// Callback data of the buttons, at most 64 bytes (Telegram limit). A short payload goes into the button itself
/// ("a|65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab"), so the button still works after a restart. A longer one, for example
/// with a long search text, stays in memory and the button keeps "~" and a number; after a restart it is outdated.
/// </summary>
public sealed class CallbackCodec : IDisposable
{
    public const int MaxBytes = 64;
    public const char Separator = '|';

    private const char StoredPrefix = '~';
    private const int MaxStoredPayloads = 10_000;

    private static readonly TimeSpan StoredLifetime = TimeSpan.FromDays(1);

    private readonly MemoryCache _stored = new(new MemoryCacheOptions { SizeLimit = MaxStoredPayloads });
    private long _nextId;

    /// <summary>
    /// Only the last argument may hold the separator: <see cref="CallbackPayload.Arguments"/> keeps it.
    /// </summary>
    public string Encode(string action, params string[] arguments)
    {
        var payload = string.Join(Separator, arguments.Prepend(action));
        if (Encoding.UTF8.GetByteCount(payload) <= MaxBytes)
        {
            return payload;
        }

        var id = $"{StoredPrefix}{Interlocked.Increment(ref _nextId)}";
        _stored.Set(id, payload, new MemoryCacheEntryOptions { Size = 1, SlidingExpiration = StoredLifetime });
        return id;
    }

    /// <summary>
    /// False when a stored payload is gone (restart, expiry).
    /// </summary>
    public bool TryDecode(string data, out CallbackPayload payload)
    {
        var text = data;
        if (data.StartsWith(StoredPrefix) && !_stored.TryGetValue(data, out text))
        {
            payload = new CallbackPayload(string.Empty, string.Empty);
            return false;
        }

        var parts = text!.Split(Separator, 2);
        payload = new CallbackPayload(parts[0], parts.Length > 1 ? parts[1] : string.Empty);
        return true;
    }

    public void Dispose()
    {
        _stored.Dispose();
    }
}

/// <summary>
/// The decoded button: what to do and its arguments.
/// </summary>
public sealed record CallbackPayload(string Action, string Arguments)
{
    /// <summary>
    /// Null when there are not exactly <paramref name="count"/> arguments; the last one keeps any separators.
    /// </summary>
    public string[]? Split(int count)
    {
        var parts = Arguments.Split(CallbackCodec.Separator, count);
        return parts.Length == count ? parts : null;
    }
}
