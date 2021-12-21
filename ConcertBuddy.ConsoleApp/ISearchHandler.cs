﻿using MusicSearcher.Model;
using SetlistFmAPI.Models;

namespace ConcertBuddy.ConsoleApp
{
    public interface ISearchHandler
    {
        Task<IEnumerable<MusicArtist>> SearchArtistsByName(string artistName, int limit = 5);

        Task<MusicArtist> SearchArtistByMBID(string mbid);

        Task<Setlists> SearchArtistSetlists(string artistName, int page = 1);

        string SearchLyric(string artistName, string songName);
    }
}