using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;

namespace ConcertBuddy.ConsoleApp.Search
{
    public interface ISearchHandler
    {
        Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0);

        Task<MusicArtistBase> SearchArtistByMBID(string mbid);

        Task<SetlistNet.Models.ArrayResult.Setlists> SearchArtistSetlists(string mbid, int page = 1);

        Task<SetlistNet.Models.Setlist> SearchSetlist(string setlistId);

        Task<MusicLyric> SearchLyric(string artistName, string songName);

        Task<MusicTrackBase> SearchTrack(string artistName, string trackName);

        Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID);

        Task<MusicRecording> SearchRecordByName(string artistMBID, string recordingName);
    }
}
