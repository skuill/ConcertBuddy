using Hqub.MusicBrainz.API.Entities;
using LyricsScraperNET;
using LyricsScraperNET.Models.Requests;
using LyricsScraperNET.Models.Responses;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model.Abstract;
using SetlistFmAPI;
using SetlistFmAPI.Models;

namespace ConcertBuddy.ConsoleApp.Search
{
    internal class SearchHandler : ISearchHandler
    {
        private readonly ILogger<ISearchHandler> _logger;
        private readonly IMusicSearcherClient _musicSearcherClient;
        private readonly ISetlistFmClient _setlistFmClient;
        private readonly ILyricsScraperClient _lyricsScraperClient;

        public SearchHandler(ILogger<ISearchHandler> logger,
            IMusicSearcherClient musicSearcherClient,
            ISetlistFmClient setlistFmClient,
            ILyricsScraperClient lyricsScraperClient)
        {
            _logger = logger;
            if (Configuration.IsLastFmAvailable())
                musicSearcherClient.WithLastFmClient(Configuration.LastFmApiKey, Configuration.LastFmApiSecret);
            if (Configuration.IsSpotifyAvailable())
                musicSearcherClient.WithSpotifyClient(Configuration.SpotifyClientID, Configuration.SpotifyClientSecret);
            if (Configuration.IsYandexAvailable())
                musicSearcherClient.WithYandexClient(Configuration.YandexToken);
            musicSearcherClient.WithMemoryCache();
            _musicSearcherClient = musicSearcherClient;

            setlistFmClient.WithApiKey(Configuration.SetlistFmApiKey);
            _setlistFmClient = setlistFmClient;

            _lyricsScraperClient = lyricsScraperClient;
        }

        public Task<Setlists> SearchArtistSetlists(string mbid, int page = 1)
        {
            return _setlistFmClient.SearchArtistSetlists(mbid, page);
        }

        public Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0)
        {
            return _musicSearcherClient.SearchArtistsByName(artistName, limit: limit, offset: offset);
        }

        public Task<SearchResult> SearchLyric(string artistName, string songName)
        {
            var searchRequest = new ArtistAndSongSearchRequest(artistName, songName);
            return _lyricsScraperClient.SearchLyricAsync(searchRequest);
        }

        public Task<MusicArtistBase> SearchArtistByMBID(string mbid)
        {
            return _musicSearcherClient.SearchArtistByMBID(mbid);
        }

        public Task<Setlist> SearchSetlist(string setlistId)
        {
            return _setlistFmClient.SearchSetlist(setlistId);
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
    }
}
