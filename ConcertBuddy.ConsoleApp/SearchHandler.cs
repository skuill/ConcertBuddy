using LyricsScraper;
using LyricsScraper.Abstract;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model;
using SetlistFmAPI;
using SetlistFmAPI.Models;

namespace ConcertBuddy.ConsoleApp
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
            ILyricGetter lyricGetter
            )
        {
            _logger = logger;
            musicSearcherClient.WithLastFmClient(Configuration.LastFmApiKey, Configuration.LastFmApiSecret);
            musicSearcherClient.WithSpotifyClient(Configuration.SpotifyClientID, Configuration.SpotifyClientSecret);
            _musicSearcherClient = musicSearcherClient;

            setlistFmClient.WithApiKey(Configuration.SetlistFmApiKey);
            _setlistFmClient = setlistFmClient;
            
            _lyricGetter = lyricGetter;
            lyricsScraperUtil.AddGetter(lyricGetter);
            _lyricsScraperUtil = lyricsScraperUtil;
        }

        public Task<Setlists> SearchArtistSetlists(string artistName, int page = 1)
        {
            var artists = _setlistFmClient.SearchArtists(artistName, page).GetAwaiter().GetResult();
            if (artists == null || artists.IsEmpty())
            {
                _logger.LogError($"Can't find artist's [{artistName}] setlists from SetlistFM");
                return null;
            }

            var artist = artists.Items.FirstOrDefault();

            return _setlistFmClient.SearchArtistSetlists(artist.MBID, page);
        }

        public Task<IEnumerable<MusicArtist>> SearchArtistsByName(string artistName, int limit = 5)
        {
            return _musicSearcherClient.SearchArtistsByName(artistName, limit:limit);
        }

        public string SearchLyric(string artistName, string songName)
        {
            return _lyricsScraperUtil.SearchLyric(artistName, songName);
        }

        public Task<MusicArtist> SearchArtistByMBID(string mbid)
        {
            return _musicSearcherClient.SearchArtistByMBID(mbid);
        }
    }
}
