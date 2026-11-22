using Hqub.MusicBrainz.Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

namespace MusicSearcher.MusicBrainz;

/// <summary>
/// The answers of MusicBrainz in memory, by the request URL (#91). The client asks it before every request, so the
/// artist search, the artist by MBID and the recording search of a track come from here for a day, as with the file
/// cache before, but within a size limit and without files that nothing cleaned.
/// </summary>
internal sealed class MemoryRequestCache : IRequestCache, IDisposable
{
    internal const long DefaultSizeLimitBytes = 16 * 1024 * 1024;

    internal static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(1);

    private readonly MemoryCache _cache;
    private readonly TimeSpan _lifetime;

    public MemoryRequestCache(TimeProvider timeProvider, long sizeLimitBytes = DefaultSizeLimitBytes, TimeSpan? lifetime = null)
    {
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimitBytes, Clock = new Clock(timeProvider) });
        _lifetime = lifetime ?? DefaultLifetime;
    }

    public async Task Add(string request, Stream response)
    {
        using var copy = new MemoryStream();
        await response.CopyToAsync(copy);
        // The client reads the answer after it is cached.
        response.Seek(0, SeekOrigin.Begin);

        var bytes = copy.ToArray();
        _cache.Set(request, bytes, new MemoryCacheEntryOptions { Size = bytes.Length, AbsoluteExpirationRelativeToNow = _lifetime });
    }

    public Task<bool> Contains(string request)
    {
        return Task.FromResult(_cache.TryGetValue(request, out _));
    }

    public Task<bool> TryGetCachedItem(string request, out Stream stream)
    {
        if (_cache.TryGetValue(request, out byte[]? bytes) && bytes != null)
        {
            stream = new MemoryStream(bytes, writable: false);
            return Task.FromResult(true);
        }

        stream = null!;
        return Task.FromResult(false);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private sealed class Clock : ISystemClock
    {
        private readonly TimeProvider _timeProvider;

        public Clock(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
    }
}
