using Hqub.MusicBrainz;
using Hqub.MusicBrainz.Entities;
using LyricsScraperNET;
using LyricsScraperNET.Models.Requests;
using LyricsScraperNET.Models.Responses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MusicSearcher.Converter;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.MusicBrainz;
using MusicSearcher.MusicBrainz;
using MusicSearcher.MusicService.Abstract;
using MusicSearcher.MusicService.LastFm;
using MusicSearcher.MusicService.SetlistFm;
using MusicSearcher.MusicService.Spotify;
using MusicSearcher.MusicService.Yandex;
using System.Net;
using System.Reflection;

namespace MusicSearcher
{
    public class MusicSearcherClient : IMusicSearcherClient
    {
        private readonly ILogger<MusicSearcherClient>? _logger;

        private List<IMusicServiceClient> _musicServiceClients;

        private MusicBrainzClient _musicBrainzClient;

        private const int MEMORY_CACHE_SIZE = 256;
        private IMemoryCache? _artistMemoryCache;
        private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions;

        private readonly ILyricsScraperClient _lyricsScraperClient;

        private readonly SetlistFmServiceClient _setlistFmClient;

        private bool _isMemoryCacheEnabled;
        public bool IsMemoryCacheEnabled => _isMemoryCacheEnabled;

        public MusicSearcherClient()
        {
            _isMemoryCacheEnabled = false;
            _memoryCacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Size = 1
            };

            // Make sure that TLS 1.2 is available.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            // Get path for local file cache.
            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            _musicBrainzClient = new MusicBrainzClient()
            {
                Cache = new FileRequestCache(Path.Combine(location!, "cache"))
            };

            _musicServiceClients = new List<IMusicServiceClient>();
        }

        public MusicSearcherClient(
            ILyricsScraperClient lyricsScraperClient,
            SetlistFmServiceClient setlistFmClient,
            YandexServiceClient yandexServiceClient,
            SpotifyServiceClient spotifyServiceClient,
            LastFmServiceClient lastFmServiceClient,
            ILogger<MusicSearcherClient> logger)
            : this()
        {
            _logger = logger;
            _lyricsScraperClient = lyricsScraperClient;
            _setlistFmClient = setlistFmClient;

            _musicServiceClients.Add(yandexServiceClient);
            _musicServiceClients.Add(spotifyServiceClient);
            _musicServiceClients.Add(lastFmServiceClient);

            _logger?.LogInformation($"Enable memory cache for artist search with size {MEMORY_CACHE_SIZE}");
            _isMemoryCacheEnabled = true;
            var memoryCacheOptions = new MemoryCacheOptions { SizeLimit = MEMORY_CACHE_SIZE };
            _artistMemoryCache = new MemoryCache(memoryCacheOptions);
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
                    _logger?.LogError($"Can't find artist [{name}] with search limit {limit}");
                    return result;
                }

                result = scoreType switch
                {
                    ScoreType.MusicBrainz => artists.Select(x => new MusicArtist(new MusicBrainzMusicArtist(x))),
                    ScoreType.Levenshtein => artists.OrderByDescending(x => Levenshtein.Similarity(x.Name, name)).Select(x => new MusicArtist(new MusicBrainzMusicArtist(x))),
                    _ => new List<MusicArtist>()
                };

                _logger?.LogDebug($"Find artists for [{name}]: {string.Join("; ", result.Select(x => $"{x.Name} [score: {x.Score}]"))}");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Can't find artists by name [{name}]");
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
                    isResultFromCache = _artistMemoryCache!.TryGetValue(artistMBID, out result);
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
                    catch (NotImplementedException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Can't get artist from music service [{musicServiceClient.GetType().Name}]");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Can't get artist by mbid [{artistMBID}] from LastFM");
                return new MusicArtist();
            }

            if (IsMemoryCacheEnabled)
                _artistMemoryCache!.Set(artistMBID, result, _memoryCacheEntryOptions);

            return result;
        }

        public async Task<MusicTrackBase> SearchTrack(string artistName, string trackName)
        {
            MusicTrack result = new MusicTrack();

            foreach (var musicServiceClient in _musicServiceClients)
            {
                try
                {
                    result.Add(await musicServiceClient.SearchTrack(artistName, trackName));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Can't get track from music service [{musicServiceClient.GetType().Name}]");
                }
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<MusicRecording?> SearchRecordByName(string artistMBID, string recordingName)
        {
            try
            {
                QueryParameters<Recording> query = new QueryParameters<Recording>();
                query.Add("arid", artistMBID);
                query.Add("recording", recordingName);

                var recordings = await _musicBrainzClient.Recordings.SearchAsync(query);

                if (recordings == null || !recordings.Any())
                {
                    _logger?.LogError($"Can't find recording for artist mbid [{artistMBID}] and song name [{recordingName}]");
                    return null;
                }

                _logger?.LogDebug($"Find recording for artist [{artistMBID}] with song name [{recordingName}]");
                return recordings.First().ToInternal();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Can't find recording for artist mbid [{artistMBID}] and song name [{recordingName}]");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID)
        {
            List<MusicTrackBase> result = new List<MusicTrackBase>();

            var artist = await SearchArtistByMBID(artistMBID);

            foreach (var musicServiceClient in _musicServiceClients)
            {
                try
                {
                    result = await musicServiceClient.SearchTopTracks(artist);
                    if (result != null && result.Any())
                        return result;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Can't get top tracks from music service [{musicServiceClient.GetType().Name}]");
                }
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<MusicLyric> SearchLyric(string artistName, string songName)
        {
            var searchRequest = new ArtistAndSongSearchRequest(artistName, songName);

            var searchResult = await _lyricsScraperClient.SearchLyricAsync(searchRequest);

            if (searchResult == null
               || searchResult.ResponseStatusCode != ResponseStatusCode.Success)
            {
                _logger?.LogWarning($"Can't find lyric for track [{artistName} - {songName}]. Reason: [{searchResult?.ResponseStatusCode}]");
                return MusicLyric.Empty();
            }

            return searchResult.ToInternal();
        }

        public async Task<MusicSetlists> SearchArtistSetlists(string artistMBID, int page = 1)
        {
            return await _setlistFmClient.SearchArtistSetlists(artistMBID, page: page);
        }

        public async Task<MusicSetlist> SearchSetlist(string setlistId)
        {
            return await _setlistFmClient.SearchSetlist(setlistId);
        }

        public void Dispose()
        {
            if (_isMemoryCacheEnabled)
                _artistMemoryCache!.Dispose();
            if (_musicServiceClients != null && _musicServiceClients.Any())
            {
                foreach (var musicServiceClient in _musicServiceClients)
                {
                    if (musicServiceClient is IAsyncDisposable)
                        ((IAsyncDisposable)musicServiceClient).DisposeAsync().GetAwaiter().GetResult();
                }
                _musicServiceClients.Clear();
            }
        }
    }
}
