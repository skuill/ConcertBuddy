using LyricsScraper.Abstract;

namespace LyricsScraper
{
    public interface ILyricsScraperUtil
    {
        string SearchLyric(Uri uri);

        string SearchLyric(string artist, string song);

        void AddGetter(ILyricGetter getter);
    }
}
