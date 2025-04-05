using Hqub.MusicBrainz.Entities;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using SetlistNet;

namespace ConcertBuddy.ConsoleApp.Search
{
    internal class SearchHandler : ISearchHandler
    {
        private readonly ILogger<ISearchHandler> _logger;
        private readonly IMusicSearcherClient _musicSearcherClient;
        private readonly SetlistApi _setlistFmClient;

        public SearchHandler(ILogger<ISearchHandler> logger,
            IMusicSearcherClient musicSearcherClient,
            SetlistApi setlistFmClient)
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

            _setlistFmClient = setlistFmClient;
        }

        public Task<SetlistNet.Models.ArrayResult.Setlists> SearchArtistSetlists(string mbid, int page = 1)
        {
            return _setlistFmClient.ArtistSetlists(mbid, page: page);
        }

        public Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0)
        {
            return _musicSearcherClient.SearchArtistsByName(artistName, limit: limit, offset: offset);
        }

        public Task<MusicArtistBase> SearchArtistByMBID(string mbid)
        {
            return _musicSearcherClient.SearchArtistByMBID(mbid);
        }

        public Task<SetlistNet.Models.Setlist> SearchSetlist(string setlistId)
        {
            return _setlistFmClient.Setlist(setlistId);
        }

        public Task<MusicTrackBase> SearchTrack(string artistName, string trackName)
        {
            return _musicSearcherClient.SearchTrack(artistName, trackName);
        }

        public Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID)
        {
            return _musicSearcherClient.SearchTopTracks(artistMBID);
        }

        public Task<Recording> SearchSongByName(string artistMBID, string name)
        {
            return _musicSearcherClient.SearchSongByName(artistMBID, name);
        }

        public Task<Recording> SearchSongByMBID(string songMBID)
        {
            return _musicSearcherClient.SearchSongByMBID(songMBID);
        }

        public Task<MusicLyric> SearchLyric(string artistName, string songName)
        {
            return _musicSearcherClient.SearchLyric(artistName, songName);
        }
    }
}
