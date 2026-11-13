namespace MusicSearcher;

/// <summary>
/// A music service did not answer (down, busy, timeout), so "nothing found" would be a lie.
/// The bot shows "Try again" for it instead.
/// </summary>
public sealed class MusicServiceUnavailableException : Exception
{
    public MusicServiceUnavailableException(string service, Exception innerException)
        : base($"{service} is unavailable: {innerException.Message}", innerException)
    {
        Service = service;
    }

    public string Service { get; }
}
