using Microsoft.Extensions.Time.Testing;

namespace ConcertBuddy.ConsoleApp.Tests.Host;

internal sealed class CountingTimeProvider : FakeTimeProvider
{
    private int _timersCreated;

    public int TimersCreated => Volatile.Read(ref _timersCreated);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        Interlocked.Increment(ref _timersCreated);
        return timer;
    }
}
