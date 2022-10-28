using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.Yandex;
using MusicSearcher.MusicService.Abstract;
using Yandex.Music.Api.Models.Common;
using Yandex.Music.Client;

namespace MusicSearcher.MusicService.Yandex
{
    internal class YandexServiceClient: IMusicServiceClient
    {
        private YandexMusicClient _yandexClient;
        private AvailableSearchType availableSearch = AvailableSearchType.All;

        public MusicServiceType MusicServiceType => MusicServiceType.Yandex;

        [Obsolete("Starting with version 2.0.0, the library no longer provides interfaces for working with OAuth Yandex and Yandex.Passport.")]
        public YandexServiceClient(string login, string password)
        {
            try
            {
                _yandexClient = new YandexMusicClient();
                _yandexClient.Authorize(login, password);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// API access token doc: https://yandex-music.readthedocs.io/en/main/#id4.
        /// How to get token: https://github.com/MarshalX/yandex-music-api/discussions/513
        /// </summary>
        public YandexServiceClient(string token)
        {
            try
            {
                _yandexClient = new YandexMusicClient();
                _yandexClient.Authorize(token);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task<MusicArtistBase> GetArtistByMBID(string mbid)
        {
            throw new NotImplementedException();
        }

        public Task<List<MusicTrackBase>> SearchTopTracks(MusicArtistBase artist)
        {
            throw new NotImplementedException();
        }

        public async Task<MusicTrackBase> SearchTrack(string artistName, string trackName)
        {
            // Known search problems with external library:
            // https://github.com/K1llMan/Yandex.Music.Api/issues/6
            var searchResult = _yandexClient.Search($"{artistName} - {trackName}", YSearchType.Track);
            if (searchResult == null
                || searchResult.Tracks == null
                || searchResult.Tracks.Total == 0)
            {
                throw new Exception($"Can't search track [{trackName}] for artist [{artistName}] from Yandex.");
            }
            var searchTrackResult = searchResult.Tracks.Results.FirstOrDefault(t => t.Artists != null && t.Artists.Any(a => string.Equals(a.Name, artistName, StringComparison.OrdinalIgnoreCase)));
            if (searchTrackResult == null)
            {
                throw new Exception($"Can't get track [{trackName}] for artist [{artistName}] from Yandex.");
            }
            return new YandexMusicTrack(_yandexClient.GetTrack(searchTrackResult.Id));
        }

        public AvailableSearchType GetAvailableSearch() => availableSearch;

        public Task<MusicArtistBase> SearchArtistByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
