using Hqub.MusicBrainz.API.Entities;
using LyricsScraper;
using LyricsScraper.Abstract;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model;
using SetlistFmAPI;
using SetlistFmAPI.Models;
using SpotifyAPI.Web;

namespace ConcertBuddy.ConsoleApp.Search
{
    internal class SearchHandler : ISearchHandler
    {
        private readonly ILogger<ISearchHandler> _logger;
        private readonly IMusicSearcherClient _musicSearcherClient;
        private readonly ISetlistFmClient _setlistFmClient;
        private readonly ILyricsScraperUtil _lyricsScraperUtil;
        private readonly ILyricGetter _lyricGetter;

        public SearchHandler(ILogger<ISearchHandler> logger, 
            IMusicSearcherClient musicSearcherClient, 
            ISetlistFmClient setlistFmClient, 
            ILyricsScraperUtil lyricsScraperUtil,
            ILyricGetter lyricGetter)
        {
            _logger = logger;
            musicSearcherClient.WithLastFmClient(Configuration.LastFmApiKey, Configuration.LastFmApiSecret);
            musicSearcherClient.WithSpotifyClient(Configuration.SpotifyClientID, Configuration.SpotifyClientSecret);
            musicSearcherClient.WithMemoryCache();
            _musicSearcherClient = musicSearcherClient;

            setlistFmClient.WithApiKey(Configuration.SetlistFmApiKey);
            _setlistFmClient = setlistFmClient;
            
            _lyricGetter = lyricGetter;
            lyricsScraperUtil.AddGetter(lyricGetter);
            _lyricsScraperUtil = lyricsScraperUtil;
        }

        public Task<Setlists> SearchArtistSetlists(string mbid, int page = 1)
        {
            return _setlistFmClient.SearchArtistSetlists(mbid, page);
        }

        public Task<IEnumerable<MusicArtist>> SearchArtistsByName(string artistName, int limit = 5, int offset = 0)
        {
            return _musicSearcherClient.SearchArtistsByName(artistName, limit:limit, offset:offset);
        }

        public string SearchLyric(string artistName, string songName)
        {
            return _lyricsScraperUtil.SearchLyric(artistName, songName);
        }

        public Task<MusicArtist> SearchArtistByMBID(string mbid)
        {
            return _musicSearcherClient.SearchArtistByMBID(mbid);
        }

        public Task<Setlist> SearchSetlist(string setlistId)
        {
            return _setlistFmClient.SearchSetlist(setlistId);
        }

        public Task<FullTrack> SearchSpotifyTrack(string artistName, string trackName)
        {
            return _musicSearcherClient.SearchSpotifyTrack(artistName, trackName);
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
