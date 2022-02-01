using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Converter;
using MusicSearcher.Model;
using MusicSearcher.MusicBrainz;
using SpotifyAPI.Web;
using System.Net;
using System.Reflection;
using Yandex.Music.Api.Models.Common;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client;

namespace MusicSearcher
{
    public class MusicSearcherClient : IMusicSearcherClient
    {
        private readonly ILogger<MusicSearcherClient> _logger;

        private MusicBrainzClient _musicBrainzClient;
        private LastfmClient _lastFmClient;
        private SpotifyClient _spotifyClient;
        private YandexMusicClient _yandexClient;

        private const int MEMORY_CACHE_SIZE = 256;
        private bool _isMemoryCacheEnabled;
        private IMemoryCache _artistMemoryCache;
        private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions;

        public MusicSearcherClient(ILogger<MusicSearcherClient> logger)
        {
            _logger = logger;

            _isMemoryCacheEnabled = false;
            _memoryCacheEntryOptions = new MemoryCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Size = 1
            };

            Init();
        }

        private void Init()
        {
            // Make sure that TLS 1.2 is available.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            // Get path for local file cache.
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _musicBrainzClient = new MusicBrainzClient()
            {
                Cache = new FileRequestCache(Path.Combine(location, "cache"))
            };
        }

        // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
        public async Task<MusicArtist> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz)
        {
            return (await SearchArtistsByName(name, scoreType, 5)).FirstOrDefault();
        }

        // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
        public async Task<IEnumerable<MusicArtist>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0)
        {
            IEnumerable<MusicArtist> result = new List<MusicArtist>();
            try
            {
                // By default, search results will be ordered by score, so to get the
                // best match you could do artists.Items.First(). Sometimes this method
                // won't work (example: search for 'U2').
                var artists = await _musicBrainzClient.Artists.SearchAsync(name.Quote(), limit, offset);
                if (artists == null || !artists.Any())
                {
                    _logger.LogError($"Can't find artist {name} with search limit {limit}");
                    return result;
                }

                result = scoreType switch
                {
                    ScoreType.MusicBrainz => artists.Select(x => new MusicArtist() { MusicBrainzArtist = x }),
                    ScoreType.Levenshtein => artists.OrderByDescending(x => Levenshtein.Similarity(x.Name, name)).Select(x => new MusicArtist() { MusicBrainzArtist = x }),
                    _ => new List<MusicArtist>()
                };

                _logger.LogDebug($"Find artists for [{name}]: {string.Join("; ", result.Select(x => $"{x.Name} [score: {x.Score}]"))}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't find artists by name [{name}]");
            }
            return result;
        }

        public async Task<MusicArtist> SearchArtistByMBID(string artistMBID)
        {
            var result = new MusicArtist();
            try
            {
                bool isResultFromCache = false;
                if (IsMemoryCacheEnabled())
                    isResultFromCache = _artistMemoryCache.TryGetValue(artistMBID, out result);
                if (isResultFromCache)
                    return result;

                result = new MusicArtist();

                var musicBrainzArtistTask = _musicBrainzClient.Artists.GetAsync(artistMBID);
                var lastFmArtistTask = GetLastFmArtist(artistMBID);

                result.MusicBrainzArtist = await musicBrainzArtistTask;
                if (IsLastFmClientEnabled())
                {
                    result.LastFmArtist = await lastFmArtistTask;
                }
                if (IsSpotifyClientEnabled())
                {
                    result.SpotifyArtist = await SearchSpotifyArtist(result.Name);
                }

            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't get artist by mbid [{artistMBID}] from LastFM");
                return new MusicArtist();
            }

            if (IsMemoryCacheEnabled())
                _artistMemoryCache.Set(artistMBID, result, _memoryCacheEntryOptions);

            return result;
        }

        public async Task<MusicTrack> SearchTrack(string artistName, string trackName)
        {
            MusicTrack result = new MusicTrack();

            if (IsSpotifyClientEnabled())
            {
                result.SpotifyTrack = await SearchSpotifyTrack(artistName, trackName);
            }
            if (IsYandexClientEnabled())
            {
                result.YandexTrack = await SearchYandexTrack(artistName, trackName);
            }
            return result;
        }

        private async Task<YTrack> SearchYandexTrack(string artistName, string trackName)
        {
            try
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
                return _yandexClient.GetTrack(searchTrackResult.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't search track [{trackName}] for artist [{artistName}] from Yandex.");
                return await Task.FromResult<YTrack>(null);
            }
        }

        private async Task<FullTrack> SearchSpotifyTrack(string artistName, string trackName)
        {
            try
            {
                var searchTrack = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, $"{artistName} - {trackName}"));
                if (searchTrack == null
                    || searchTrack.Tracks == null
                    || searchTrack.Tracks.Total == 0)
                {
                    throw new Exception($"Can't get track [{trackName}] for artist [{artistName}] from Spotify.");
                }
                return searchTrack.Tracks.Items.First(t => t.Artists != null && t.Artists.Any(a => string.Equals(a.Name, artistName)));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't search track [{trackName}] for artist [{artistName}] from Spotify.");
                return await Task.FromResult<FullTrack>(null);
            }
        }

        private async Task<FullArtist> SearchSpotifyArtist(string artistName)
        {
            try
            {
                var searchArtist = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Artist, artistName));
                if (searchArtist == null
                    || searchArtist.Artists == null
                    || searchArtist.Artists.Total == 0)
                {
                    throw new Exception($"Can't get artist [{artistName}] from Spotify.");
                }
                // We can't compare artistName. For example for artist "ноганно" actual spotify name is "noganno".
                return searchArtist.Artists.Items.First();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't get artist [{artistName}] from Spotify.");
                return await Task.FromResult<FullArtist>(null);
            }
        }

        public async Task<Recording> SearchSongByName(string artistMBID, string name)
        {
            Recording result = null;
            try
            {
                QueryParameters<Recording> query = new QueryParameters<Recording>();
                query.Add("arid", artistMBID);
                query.Add("recording", name);
                var recordings = await _musicBrainzClient.Recordings.SearchAsync(query);
                if (recordings == null || !recordings.Any())
                {
                    _logger.LogError($"Can't find recording for artist mbid [{artistMBID}] and song name [{name}]");
                    return result;
                }

                _logger.LogDebug($"Find recording for artist [{artistMBID}] with song name [{name}]");
                return recordings.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't find recording for artist mbid [{artistMBID}] and song name [{name}]");
            }
            return result;
        }

        private async Task<IEnumerable<FullTrack>> GetSpotifyTopTracks(FullArtist artist, string country)
        {
            var topTracks = await _spotifyClient.Artists.GetTopTracks(artist.Id, new ArtistsTopTracksRequest(country));
            if (topTracks != null && topTracks.Tracks != null && topTracks.Tracks.Any())
            {
                return topTracks.Tracks;
            }
            else
            {
                _logger.LogError($"Can't get top tracks from Spotify for artist [{artist.Name}] with id [{artist.Id}]");
                return await Task.FromResult<IEnumerable<FullTrack>>(null);
            }
        }

        public async Task<Recording> SearchSongByMBID(string songMBID)
        {
            return await _musicBrainzClient.Recordings.GetAsync(songMBID);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MusicTrack>> SearchTopTracks(string artistName, string country)
        {
            List<MusicTrack> result = new List<MusicTrack>();

            if (IsSpotifyClientEnabled())
            {
                var spotifyArtist = await SearchSpotifyArtist(artistName);
                if (spotifyArtist != null)
                {
                    string formattedCountry = RegionConverter.ConvertToTwoLetterISO(country);
                    var spotifyTracks = await GetSpotifyTopTracks(spotifyArtist, formattedCountry);
                    if (spotifyTracks != null )
                    {
                        return spotifyTracks.Select(t => new MusicTrack() { SpotifyTrack = t });
                    }
                }
            }
            return result;
        }

        public void WithLastFmClient(string apiKey, string secret)
        {
            _logger.LogInformation($"Enable LastFM client for artist search");
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning($"Please set apiKey [{apiKey}] and secret [{secret}] properly!");
                return;
            }
            try
            {
                _lastFmClient = new LastfmClient(apiKey, secret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't initialize LastFM client!");
            }
        }

        public async Task WithSpotifyClient(string cliendID, string clientSecret)
        {
            _logger.LogInformation($"Enable Spotify client for artist search");
            if (string.IsNullOrEmpty(cliendID) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning($"Please set cliendID [{cliendID}] and clientSecret [{clientSecret}] properly!");
                return;
            }
            try
            {
                var config = SpotifyClientConfig
                    .CreateDefault()
                    .WithRetryHandler(new SimpleRetryHandler() { RetryAfter = TimeSpan.FromSeconds(1) })
                    .WithAuthenticator(new ClientCredentialsAuthenticator(cliendID, clientSecret));

                _spotifyClient = new SpotifyClient(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't initialize Spotify client!");
            }
        }

        public void WithMemoryCache()
        {
            _logger.LogInformation($"Enable memory cache for artist search with size {MEMORY_CACHE_SIZE}");
            _isMemoryCacheEnabled = true;
            var memoryCacheOptions = new MemoryCacheOptions { SizeLimit = MEMORY_CACHE_SIZE };
            _artistMemoryCache = new MemoryCache(memoryCacheOptions);
        }

        public async Task WithYandexClient(string login, string password)
        {
            _logger.LogInformation($"Enable Yandex client for artist search");
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning($"Please set Yandex credentials: user login [{login}] and password [{password}] properly!");
                return;
            }
            try
            {
                _yandexClient = new YandexMusicClient();
                _yandexClient.Authorize(login, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't initialize Yandex client!");
            }
        }

        private async Task<LastArtist> GetLastFmArtist(string mbid)
        {
            LastArtist result = null;
            if (IsLastFmClientEnabled())
            {
                var lastFmArtist = await _lastFmClient.Artist.GetInfoByMbidAsync(mbid);
                if (lastFmArtist != null && lastFmArtist.Success)
                {
                    result = lastFmArtist.Content;
                }
                else
                {
                    _logger.LogError($"Can't get artist by mbid [{mbid}] from LastFM. Status: {lastFmArtist?.Status}.");
                }
            }
            return result;
        }

        private bool IsLastFmClientEnabled() => _lastFmClient != null;

        private bool IsSpotifyClientEnabled() => _spotifyClient != null;

        private bool IsYandexClientEnabled() => _yandexClient != null;

        private bool IsMemoryCacheEnabled() => _isMemoryCacheEnabled;

        public void Dispose()
        {
            if (_isMemoryCacheEnabled)
                _artistMemoryCache.Dispose();
        }
    }
}
