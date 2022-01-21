using LyricsScraper.Abstract;
using Microsoft.Extensions.Logging;

namespace LyricsScraper
{
    public class LyricsScraperUtil: ILyricsScraperUtil
    {
        private List<ILyricGetter> _lyricGetters;
        private readonly ILogger<LyricsScraperUtil> _logger;

        public LyricsScraperUtil(ILogger<LyricsScraperUtil> logger)
        {
            _logger = logger;
        }

        public string SearchLyric(Uri uri)
        {
            if (IsEmptyGetters())
            {
                _logger.LogError("Empty getter list! Please set any getter frist.");
            }
            foreach (var lyricGetter in _lyricGetters)
            {
                var lyric = lyricGetter.SearchLyric(uri);
                if (!string.IsNullOrEmpty(lyric))
                {
                    return lyric;
                }
                _logger.LogWarning($"Can't find lyric by getter: {lyricGetter}.");
            }
            _logger.LogError($"Can't find lyrics for {uri}.");
            return null;
        }

        public string SearchLyric(string artist, string song)
        {
            if (IsEmptyGetters())
            {
                _logger.LogError("Empty getter list! Please set any getter frist.");
            }
            foreach (var lyricGetter in _lyricGetters)
            {
                var lyric = lyricGetter.SearchLyric(artist, song);
                if (!string.IsNullOrEmpty(lyric))
                {
                    return lyric;
                }
                _logger.LogWarning($"Can't find lyric by getter: {lyricGetter}.");
            }
            _logger.LogError($"Can't find lyrics! Artist: {artist}. Song: {song}");
            return null;
        }

        public async Task<string> SearchLyricAsync(Uri uri)
        {
            if (IsEmptyGetters())
            {
                _logger.LogError("Empty getter list! Please set any getter frist.");
            }
            foreach (var lyricGetter in _lyricGetters)
            {
                var lyric = await lyricGetter.SearchLyricAsync(uri);
                if (!string.IsNullOrEmpty(lyric))
                {
                    return lyric;
                }
                _logger.LogWarning($"Can't find lyric by getter: {lyricGetter}.");
            }
            _logger.LogError($"Can't find lyrics for {uri}.");
            return null;
        }

        public async Task<string> SearchLyricAsync(string artist, string song)
        {
            if (IsEmptyGetters())
            {
                _logger.LogError("Empty getter list! Please set any getter frist.");
            }
            foreach (var lyricGetter in _lyricGetters)
            {
                var lyric = await lyricGetter.SearchLyricAsync(artist, song);
                if (!string.IsNullOrEmpty(lyric))
                {
                    return lyric;
                }
                _logger.LogWarning($"Can't find lyric by getter: {lyricGetter}.");
            }
            _logger.LogError($"Can't find lyrics! Artist: {artist}. Song: {song}");
            return null;
        }

        public void AddGetter(ILyricGetter getter)
        {
            if (IsEmptyGetters())
                _lyricGetters = new List<ILyricGetter>();
            _lyricGetters.Add(getter);
        }

        private bool IsEmptyGetters() => _lyricGetters == null || !_lyricGetters.Any();

    }
}