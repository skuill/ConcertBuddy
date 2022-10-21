using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.MusicBrainz;
using MusicSearcher.MusicBrainz;
using MusicSearcher.MusicService.Abstract;
using MusicSearcher.MusicService.LastFm;
using MusicSearcher.MusicService.Spotify;
using MusicSearcher.MusicService.Yandex;
using System.Net;
using System.Reflection;

namespace MusicSearcher
{
    public class MusicSearcherClient : IMusicSearcherClient
    {
        private readonly ILogger<MusicSearcherClient> _logger;

        private List<IMusicServiceClient> _musicServiceClients;

        private MusicBrainzClient _musicBrainzClient;

        private const int MEMORY_CACHE_SIZE = 256;
        private IMemoryCache _artistMemoryCache;
        private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions;


        private bool _isLastFmClientEnabled = false;
        public bool IsLastFmClientEnabled => _isLastFmClientEnabled;

        private bool _isSpotifyClientEnabled = false;
        private bool IsSpotifyClientEnabled => _isSpotifyClientEnabled;

        private bool _isYandexClientEnabled = false;
        private bool IsYandexClientEnabled => _isYandexClientEnabled;

        private bool _isMemoryCacheEnabled;
        public bool IsMemoryCacheEnabled => _isMemoryCacheEnabled;

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

            _musicServiceClients = new List<IMusicServiceClient>();
        }

        // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
        public async Task<MusicArtistBase> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz)
        {
            return (await SearchArtistsByName(name, scoreType, 5)).FirstOrDefault();
        }

        // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
        public async Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0)
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
                    ScoreType.MusicBrainz => artists.Select(x => new MusicArtist(new MusicBrainzMusicArtist(x))),
                    ScoreType.Levenshtein => artists.OrderByDescending(x => Levenshtein.Similarity(x.Name, name)).Select(x => new MusicArtist(new MusicBrainzMusicArtist(x))),
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

        public async Task<MusicArtistBase> SearchArtistByMBID(string artistMBID)
        {
            var result = new MusicArtist();
            try
            {
                bool isResultFromCache = false;
                if (IsMemoryCacheEnabled)
                    isResultFromCache = _artistMemoryCache.TryGetValue(artistMBID, out result);
                if (isResultFromCache)
                    return result;

                result = new MusicArtist();

                result.Add(new MusicBrainzMusicArtist(await _musicBrainzClient.Artists.GetAsync(artistMBID)));

                foreach (var musicServiceClient in _musicServiceClients.OrderByDescending(x => x.GetAvailableSearch()))
                {
                    try
                    {
                        switch (musicServiceClient.GetAvailableSearch())
                        {
                            case AvailableSearchType.MBID:
                            case AvailableSearchType.All:
                                result.Add(await musicServiceClient.GetArtistByMBID(artistMBID));
                                break;
                            case AvailableSearchType.Name:
                                result.Add(await musicServiceClient.SearchArtistByName(result.Name));
                                break;

                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Can't get artist from music service [{musicServiceClient.GetType().Name}]");
                    }
                }
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't get artist by mbid [{artistMBID}] from LastFM");
                return new MusicArtist();
            }

            if (IsMemoryCacheEnabled)
                _artistMemoryCache.Set(artistMBID, result, _memoryCacheEntryOptions);

            return result;
        }

        public async Task<MusicTrack> SearchTrack(string artistName, string trackName)
        {
            MusicTrack result = new MusicTrack();

            foreach (var musicServiceClient in _musicServiceClients)
            {
                try
                {
                    await musicServiceClient.SearchTrack(result, artistName, trackName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Can't get track from music service [{musicServiceClient.GetType().Name}]");
                }
            }
            return result;
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


        public async Task<Recording> SearchSongByMBID(string songMBID)
        {
            return await _musicBrainzClient.Recordings.GetAsync(songMBID);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MusicTrack>> SearchTopTracks(string artistMBID)
        {
            List<MusicTrack> result = new List<MusicTrack>();

            var artist = await SearchArtistByMBID(artistMBID);

            foreach (var musicServiceClient in _musicServiceClients)
            {
                try
                {
                    result = await musicServiceClient.SearchTopTracks(artist);
                    if (result != null && result.Any())
                        return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Can't get top tracks from music service [{musicServiceClient.GetType().Name}]");
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
                _musicServiceClients.Add(new LastFmServiceClient(apiKey, secret));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't initialize LastFM client!");
            }
            _isLastFmClientEnabled = true;
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
                _musicServiceClients.Add(new SpotifyServiceClient(cliendID, clientSecret));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't initialize Spotify client!");
            }
            _isSpotifyClientEnabled = true;
        }

        public void WithMemoryCache()
        {
            _logger.LogInformation($"Enable memory cache for artist search with size {MEMORY_CACHE_SIZE}");
            _isMemoryCacheEnabled = true;
            var memoryCacheOptions = new MemoryCacheOptions { SizeLimit = MEMORY_CACHE_SIZE };
            _artistMemoryCache = new MemoryCache(memoryCacheOptions);
        }

        public async Task WithYandexClient(string token)
        {
            _logger.LogInformation($"Enable Yandex client for artist search");
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning($"Please set Yandex credentials: token [{token}]!");
                return;
            }
            try
            {
                _musicServiceClients.Add(new YandexServiceClient(token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't initialize Yandex client!");
            }
            _isYandexClientEnabled = true;
        }


        public void Dispose()
        {
            if (_isMemoryCacheEnabled)
                _artistMemoryCache.Dispose();
        }
    }
}
