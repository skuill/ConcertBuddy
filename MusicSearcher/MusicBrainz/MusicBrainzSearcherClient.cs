using Hqub.MusicBrainz.API;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using System.Net;
using System.Reflection;

namespace MusicSearcher.MusicBrainz
{
    public class MusicBrainzSearcherClient : IMusicSearcherClient
    {
        private readonly ILogger<MusicBrainzSearcherClient> _logger;
        private MusicBrainzClient _musicBrainzClient;

        public MusicBrainzSearcherClient(ILogger<MusicBrainzSearcherClient> logger)
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
        public async Task<string> SearchArtist(string name, ScoreType scoreType = ScoreType.MusicBrainz)
        {
            return (await SearchArtists(name, scoreType, 5)).FirstOrDefault();
        }

        // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
        public async Task<IEnumerable<string>> SearchArtists(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5)
        {
            // Search for an artist by name (limit to 20 matches).
            return (await SearchArtistsWithScore(name, scoreType, limit)).Select(x => x.Key);
        }

        // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
        public async Task<IDictionary<string, int>> SearchArtistsWithScore(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5)
        {
            IDictionary<string, int> result = new Dictionary<string, int>();
            try
            {
                // By default, search results will be ordered by score, so to get the
                // best match you could do artists.Items.First(). Sometimes this method
                // won't work (example: search for 'U2').
                var artists = await _musicBrainzClient.Artists.SearchAsync(name.Quote(), limit);
                if (artists == null || !artists.Any())
                {
                    _logger.LogError($"Can't find artist {name} with search limit {limit}");
                    return new Dictionary<string, int>();
                }

                result = scoreType switch
                {
                    ScoreType.MusicBrainz => artists.ToDictionary(x => x.Name, x => x.Score),
                    ScoreType.Levenshtein => artists.Items.Select(x => new { x.Name, Score = Levenshtein.Similarity(x.Name, name) })
                            .ToDictionary(x => x.Name, x => x.Score),
                    _ => new Dictionary<string, int>()
                };

                _logger.LogDebug($"Find artist for name {name}:\r\n{string.Join("; ", result.Select(x => $"name: {x.Key}, score: {x.Value}"))}");
                return result;
            }
            catch (AggregateException e)
            {
                foreach (var item in e.Flatten().InnerExceptions)
                {
                    if (item.InnerException == null)
                    {
                        _logger.LogError(item.Message);
                    }
                    else
                    {
                        // Display inner exception.
                        _logger.LogError(item.InnerException.Message);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return result;
        }
    }
}
