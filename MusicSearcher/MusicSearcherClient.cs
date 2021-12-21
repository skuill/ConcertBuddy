using Hqub.MusicBrainz.API;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Objects;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model;
using MusicSearcher.MusicBrainz;
using SpotifyAPI.Web;
using System.Net;
using System.Reflection;

namespace MusicSearcher
{
    public class MusicSearcherClient : IMusicSearcherClient
    {
        private readonly ILogger<MusicSearcherClient> _logger;
        private MusicBrainzClient _musicBrainzClient;
        private LastfmClient _lastFmClient;
        private SpotifyClient _spotifyClient;

        public MusicSearcherClient(ILogger<MusicSearcherClient> logger)
        {
            _logger = logger;
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
        public async Task<IEnumerable<MusicArtist>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5)
        {
            IEnumerable<MusicArtist> result = new List<MusicArtist>();
            try
            {
                // By default, search results will be ordered by score, so to get the
                // best match you could do artists.Items.First(). Sometimes this method
                // won't work (example: search for 'U2').
                var artists = await _musicBrainzClient.Artists.SearchAsync(name.Quote(), limit);
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

        public async Task<MusicArtist> SearchArtistByMBID(string mbid)
        {
            var result = new MusicArtist();
            try
            {
                result.MusicBrainzArtist = await _musicBrainzClient.Artists.GetAsync(mbid);

                result.LastFmArtist = await GetLastFmArtist(mbid);

                result.SpotifyArtist = await GetSpotifyArtist(result.Name);
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't get artist by mbid [{mbid}] from LastFM");
            }

            return result;
        }

        public void WithLastFmClient(string apiKey, string secret)
        {
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

        private async Task<FullArtist> GetSpotifyArtist(string artistName)
        {
            FullArtist result = null;
            if (IsSpotifyClientEnabled())
            {
                var searchArtist = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Artist, artistName));
                if (searchArtist == null
                    || searchArtist.Artists == null
                    || searchArtist.Artists.Total == 0)
                {
                    _logger.LogError($"Can't get artist by name [{artistName}] from Spotify.");
                    return null;
                }
                result = searchArtist.Artists.Items.First(x => string.Equals(x.Name, artistName));
            }
            return result;
        }

        private bool IsLastFmClientEnabled() => _lastFmClient != null;

        private bool IsSpotifyClientEnabled() => _spotifyClient != null;
    }
}
