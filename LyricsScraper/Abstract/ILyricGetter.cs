namespace LyricsScraper.Abstract
{
    public interface ILyricGetter
    {
        string SearchLyric(Uri uri);

        string SearchLyric(string artist, string song);

        void WithParser(IParser parser);

        void WithWebClient(IWebClient webClient);
    }
}
