using Hqub.MusicBrainz.API.Entities;
using MusicSearcher.Model.Abstract;
using SetlistFmAPI.Models;

namespace ConcertBuddy.ConsoleApp.Search
{
    public interface ISearchHandler
    {
        Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0);

        Task<MusicArtistBase> SearchArtistByMBID(string mbid);

        Task<Setlists> SearchArtistSetlists(string mbid, int page = 1);

        Task<Setlist> SearchSetlist(string setlistId);

        Task<string> SearchLyric(string artistName, string songName);

        Task<MusicTrackBase> SearchTrack(string artistName, string trackName);

        Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID);

        Task<Recording> SearchSongByName(string artistMBID, string name);

        Task<Recording> SearchSongByMBID(string songMBID);
    }
}
