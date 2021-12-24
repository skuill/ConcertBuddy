using MusicSearcher.Model;
using MusicSearcher.MusicBrainz;
using SpotifyAPI.Web;

namespace MusicSearcher.Abstract
{
    public interface IMusicSearcherClient
    {
        Task<MusicArtist> SearchArtistByMBID(string mbid);

        Task<MusicArtist> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz);

        Task<IEnumerable<MusicArtist>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0);

        void WithLastFmClient(string apiKey, string secret);

        Task WithSpotifyClient(string cliendID, string clientSecret);

        Task<FullTrack> SearchSpotifyTrack(string artistName, string trackName);
    }
}
