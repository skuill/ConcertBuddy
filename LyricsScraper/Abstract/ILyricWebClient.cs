namespace LyricsScraper.Abstract
{
    public interface ILyricWebClient
    {
        string Load(Uri uri);

        Task<string> LoadAsync(Uri uri);
    }
}
