using MusicSearcher.Model;
using MusicSearcher.MusicService.Abstract;
using Yandex.Music.Api.Models.Common;
using Yandex.Music.Client;

namespace MusicSearcher.MusicService.Yandex
{
    internal class YandexServiceClient: IMusicServiceClient
    {
        private YandexMusicClient _yandexClient;
        private AvailableSearchType availableSearch = AvailableSearchType.All;

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

        public Task GetArtistByMBID(MusicArtist artist, string mbid)
        {
            throw new NotImplementedException();
        }

        public Task<List<MusicTrack>> SearchTopTracks(MusicArtist artist)
        {
            throw new NotImplementedException();
        }

        public async Task SearchTrack(MusicTrack track, string artistName, string trackName)
        {
            var searchResult = _yandexClient.Search($"{artistName} - {trackName}", YSearchType.Track);
            if (searchResult == null
                || searchResult.Tracks == null
                || searchResult.Tracks.Total == 0)
            {
                throw new Exception($"Can't search track [{trackName}] for artist [{artistName}] from Yandex.");
            }
            var searchTrackResult = searchResult.Tracks.Results.FirstOrDefault(t => t.Artists != null && t.Artists.Any(a => string.Equals(a.Name, artistName)));
            if (searchTrackResult == null)
            {
                throw new Exception($"Can't get track [{trackName}] for artist [{artistName}] from Yandex.");
            }
            track.YandexTrack = _yandexClient.GetTrack(searchTrackResult.Id);
        }

        public AvailableSearchType GetAvailableSearch() => availableSearch;

        public Task SearchArtistByName(MusicArtist artist, string name)
        {
            throw new NotImplementedException();
        }
    }
}
