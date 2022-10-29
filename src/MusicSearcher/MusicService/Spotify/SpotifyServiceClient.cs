using MusicSearcher.Converter;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.Spotify;
using MusicSearcher.MusicService.Abstract;
using SpotifyAPI.Web;

namespace MusicSearcher.MusicService.Spotify
{
    internal class SpotifyServiceClient : IMusicServiceClient, IAsyncDisposable
    {
        private SpotifyClient _spotifyClient;
        private AvailableSearchType _availableSearch = AvailableSearchType.Name;

        private List<string> _availableMarkets = new List<string>();

        private PeriodicTimer _availableMarketsTimer;
        private Task _availableMarketsTimerTask;
        private CancellationTokenSource _cancellationToken;

        public MusicServiceType MusicServiceType => MusicServiceType.Spotify;

        public SpotifyServiceClient(string cliendID, string clientSecret)
        {
            try
            {
                var config = SpotifyClientConfig
                       .CreateDefault()
                       .WithRetryHandler(new SimpleRetryHandler() { RetryAfter = TimeSpan.FromSeconds(1) })
                       .WithAuthenticator(new ClientCredentialsAuthenticator(cliendID, clientSecret));

                _spotifyClient = new SpotifyClient(config);

                _cancellationToken = new CancellationTokenSource();
                _availableMarketsTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
                _availableMarketsTimerTask = HandleAvailableMarketsTimerAsync(_availableMarketsTimer, _cancellationToken.Token);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public AvailableSearchType GetAvailableSearch() => _availableSearch;

        /// <summary>
        /// Get available markets in Spotify every timer ticks
        /// </summary>
        private async Task HandleAvailableMarketsTimerAsync(PeriodicTimer timer, CancellationToken cancellationToken = default)
        {
            try
            {
                do
                {
                    var availableMarkets = await _spotifyClient.Markets.AvailableMarkets();
                    if (availableMarkets.Markets != null && availableMarkets.Markets.Any())
                    {
                        _availableMarkets = availableMarkets.Markets;
                    }
                } 
                while (!cancellationToken.IsCancellationRequested
                && await timer.WaitForNextTickAsync(cancellationToken));
            }
            catch (Exception ex)
            {
                //Handle the exception but don't propagate it
            }
        }

        public Task<MusicArtistBase> GetArtistByMBID(string mbid)
        {
            throw new NotImplementedException();
        }

        private string GetAvailableMarketForArtist(MusicArtistBase artist)
        {
            // Country code should be in ISO 3166-1
            string formattedCountry = RegionConverter.ConvertToTwoLetterISO(artist.Country);

            if (_availableMarkets == null || !_availableMarkets.Any())
                return formattedCountry;
            if (_availableMarkets.Any(x => string.Equals(x, formattedCountry, StringComparison.OrdinalIgnoreCase)))
                return formattedCountry;
            if (_availableMarkets.Contains(RegionConverter.DEFAULT_REGION_CODE))
                return RegionConverter.DEFAULT_REGION_CODE;
            return _availableMarkets.First();
        }

        public async Task<List<MusicTrackBase>> SearchTopTracks(MusicArtistBase artist)
        {
            if (artist != null && artist.IsMusicArtistExist(MusicServiceType.Spotify))
            {
                string artistMarket = GetAvailableMarketForArtist(artist);
                var spotifyTracks = await GetSpotifyTopTracks((artist.GetMusicArtistByServiceType(MusicServiceType.Spotify) as SpotifyMusicArtist).Artist, artistMarket);
                if (spotifyTracks != null)
                {
                    return spotifyTracks.Select(x => new SpotifyMusicTrack(x)).ToList<MusicTrackBase>();
                }
            }
            return null;
        }

        private async Task<IEnumerable<FullTrack>> GetSpotifyTopTracks(FullArtist artist, string artistMarket)
        {
            var topTracks = await _spotifyClient.Artists.GetTopTracks(artist.Id, new ArtistsTopTracksRequest(artistMarket));
            if (topTracks != null && topTracks.Tracks != null && topTracks.Tracks.Any())
            {
                return topTracks.Tracks;
            }
            else
            {
                throw new Exception($"Can't get top tracks from Spotify for artist [{artist.Name}] with id [{artist.Id}] in market [{artistMarket}]");
            }
        }

        public async Task<MusicTrackBase> SearchTrack(string artistName, string trackName)
        {
            var searchTrack = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, $"{artistName} - {trackName}"));
            if (searchTrack == null
                || searchTrack.Tracks == null
                || searchTrack.Tracks.Total == 0)
            {
                throw new Exception($"Can't get track [{trackName}] for artist [{artistName}] from Spotify.");
            }
            // We can't compare artistName. For example for artist "ноганно" actual spotify name is "noganno".
            return new SpotifyMusicTrack(searchTrack.Tracks.Items.First(t => t.Artists != null));
        }

        public async Task<MusicArtistBase> SearchArtistByName(string name)
        {
            var searchArtist = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Artist, name));
            if (searchArtist == null
                || searchArtist.Artists == null
                || searchArtist.Artists.Total == 0)
            {
                throw new Exception($"Can't get artist [{name}] from Spotify.");
            }

            // We can't compare artistName. For example for artist "ноганно" actual spotify name is "noganno".
            // First ty to return artist with the same name.
            if (searchArtist.Artists.Items.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                return new SpotifyMusicArtist(searchArtist.Artists.Items.First(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));
            return new SpotifyMusicArtist(searchArtist.Artists.Items.First());
        }
    
        public async ValueTask DisposeAsync()
        {
            _availableMarketsTimer.Dispose();
            await _availableMarketsTimerTask;
            GC.SuppressFinalize(this);
        }
    }
}
