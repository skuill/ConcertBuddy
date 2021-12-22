using MusicSearcher.Model;
using SetlistFmAPI.Models;

namespace ConcertBuddy.ConsoleApp.Search
{
    public interface ISearchHandler
    {
        Task<IEnumerable<MusicArtist>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0);

        Task<MusicArtist> SearchArtistByMBID(string mbid);

        Task<Setlists> SearchArtistSetlists(string artistName, int page = 1);

        string SearchLyric(string artistName, string songName);
    }
}
