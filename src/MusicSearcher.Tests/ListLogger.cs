using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MusicSearcher.Tests;

/// <summary>
/// Keeps the level and the formatted message of every line. For loggers of internal types, which FakeItEasy can not fake.
/// </summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    public IReadOnlyCollection<(LogLevel Level, string Message)> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _entries.Enqueue((logLevel, formatter(state, exception)));
    }
}
