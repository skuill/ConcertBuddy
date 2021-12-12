using LyricsScraper.Abstract;
using Microsoft.Extensions.Logging;

namespace LyricsScraper
{
    public class LyricsScraperUtil
    {
        private static List<LyricGetter> _lyricGetters;
        private static readonly ILogger<LyricsScraperUtil> _logger;

        public static string SearchLyric(Uri uri)
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

        public static string SearchLyric(string artist, string song)
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

        public static void WithGetter(LyricGetter getter)
        {
            if (IsEmptyGetters())
                _lyricGetters = new List<LyricGetter>();
            _lyricGetters.Add(getter);
        }

        private static bool IsEmptyGetters() => _lyricGetters == null || !_lyricGetters.Any();
    }
}