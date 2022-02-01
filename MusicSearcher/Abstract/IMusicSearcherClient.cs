using Hqub.MusicBrainz.API.Entities;
using MusicSearcher.Model;
using MusicSearcher.MusicBrainz;

namespace MusicSearcher.Abstract
{
    public interface IMusicSearcherClient : IDisposable
    {
        Task<MusicArtist> SearchArtistByMBID(string artistMBID);

        Task<MusicArtist> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz);

        Task<IEnumerable<MusicArtist>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0);

        Task<MusicTrack> SearchTrack(string artistName, string trackName);

        /// <summary>
        /// Return TOP tracks from available music service (Spotify,..) that popular in country <paramref name="country"/>
        /// </summary>
        /// <param name="artistName"></param>
        /// <param name="country">country code in ISO 3166-1</param>
        /// <returns></returns>
        Task<IEnumerable<MusicTrack>> SearchTopTracks(string artistName, string country);

        Task<Recording> SearchSongByName(string artistMBID, string name);

        Task<Recording> SearchSongByMBID(string songMBID);

        void WithLastFmClient(string apiKey, string secret);

        Task WithSpotifyClient(string cliendID, string clientSecret);

        Task WithYandexClient(string login, string password);

        void WithMemoryCache();
    }
}
