﻿using MusicSearcher.Model;
using SetlistFmAPI.Models;

namespace ConcertBuddy.ConsoleApp.Search
{
    public interface ISearchHandler
    {
        Task<IEnumerable<MusicArtist>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0);

        Task<MusicArtist> SearchArtistByMBID(string mbid);

        Task<Setlists> SearchArtistSetlists(string mbid, int page = 1);

        Task<Setlist> SearchSetlist(string setlistId);

        string SearchLyric(string artistName, string songName);
    }
}
