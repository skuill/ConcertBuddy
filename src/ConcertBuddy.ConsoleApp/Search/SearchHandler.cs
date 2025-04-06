using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;

namespace ConcertBuddy.ConsoleApp.Search
{
    internal class SearchHandler : ISearchHandler
    {
        private readonly ILogger<ISearchHandler> _logger;
        private readonly IMusicSearcherClient _musicSearcherClient;

        public SearchHandler(ILogger<ISearchHandler> logger,
            IMusicSearcherClient musicSearcherClient)
        {
            _logger = logger;

            if (Configuration.IsLastFmAvailable())
                musicSearcherClient.WithLastFmClient(Configuration.LastFmApiKey!, Configuration.LastFmApiSecret!);
            if (Configuration.IsSpotifyAvailable())
                musicSearcherClient.WithSpotifyClient(Configuration.SpotifyClientID!, Configuration.SpotifyClientSecret!);
            if (Configuration.IsYandexAvailable())
                musicSearcherClient.WithYandexClient(Configuration.YandexToken!);

            musicSearcherClient.WithMemoryCache();
            _musicSearcherClient = musicSearcherClient;
        }

        public Task<MusicSetlists> SearchArtistSetlists(string artistMBID, int page = 1)
        {
            return _musicSearcherClient.SearchArtistSetlists(artistMBID, page: page);
        }

        public Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0)
        {
            return _musicSearcherClient.SearchArtistsByName(artistName, limit: limit, offset: offset);
        }

        public Task<MusicArtistBase> SearchArtistByMBID(string mbid)
        {
            return _musicSearcherClient.SearchArtistByMBID(mbid);
        }

        public Task<MusicSetlist> SearchSetlist(string setlistId)
        {
            return _musicSearcherClient.SearchSetlist(setlistId);
        }

        public Task<MusicTrackBase> SearchTrack(string artistName, string trackName)
        {
            return _musicSearcherClient.SearchTrack(artistName, trackName);
        }

        public Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID)
        {
            return _musicSearcherClient.SearchTopTracks(artistMBID);
        }

        public Task<MusicRecording> SearchRecordByName(string artistMBID, string recordingName)
        {
            return _musicSearcherClient.SearchRecordByName(artistMBID, recordingName);
        }

        public Task<MusicLyric> SearchLyric(string artistName, string songName)
        {
            return _musicSearcherClient.SearchLyric(artistName, songName);
        }
    }
}
