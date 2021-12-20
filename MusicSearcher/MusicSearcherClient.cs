using Hqub.MusicBrainz.API;
using Hqub.MusicBrainz.API.Entities;
using IF.Lastfm.Core.Api;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.Model;
using MusicSearcher.MusicBrainz;
using System.Net;
using System.Reflection;

namespace MusicSearcher
{
    public class MusicSearcherClient : IMusicSearcherClient
    {
        private readonly ILogger<MusicSearcherClient> _logger;
        private MusicBrainzClient _musicBrainzClient;
        private LastfmClient _lastFmClient;

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

                if (IsLastFmClientEnabled())
                {
                    var lastFmArtist = await _lastFmClient.Artist.GetInfoByMbidAsync(mbid);
                    if (lastFmArtist != null && lastFmArtist.Success)
                    {
                        result.LastFmArtist = lastFmArtist.Content;
                    }
                    else
                    {
                        _logger.LogError($"Can't get artist by mbid [{mbid}] from LastFM. Status: {lastFmArtist?.Status}.");
                    }
                }
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
                _logger.LogWarning($"Please set apiKey [{apiKey}] and secret[{secret}] properly!");
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

        private bool IsLastFmClientEnabled() => _lastFmClient != null;
    }
}
