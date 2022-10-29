namespace LyricsScraper.Abstract
{
    public interface ILyricGetter
    {
        string SearchLyric(Uri uri);

        string SearchLyric(string artist, string song);

        Task<string> SearchLyricAsync(Uri uri);

        Task<string> SearchLyricAsync(string artist, string song);

        void WithParser(ILyricParser parser);

        void WithWebClient(ILyricWebClient webClient);
    }
}
