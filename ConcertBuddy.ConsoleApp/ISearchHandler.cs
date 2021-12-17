using SetlistFmAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcertBuddy.ConsoleApp
{
    public interface ISearchHandler
    {
        Task<IDictionary<string, int>> SearchArtistsWithScore(string artistName, int limit = 5);

        Task<Setlists> SearchArtistSetlists(string artistName, int page = 1);

        string SearchLyric(string artistName, string songName);
    }
}
