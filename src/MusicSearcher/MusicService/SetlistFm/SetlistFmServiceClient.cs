using Microsoft.Extensions.Logging;
using MusicSearcher.Converter;
using MusicSearcher.Model;
using SetlistNet;

namespace MusicSearcher.MusicService.SetlistFm
{
    public sealed class SetlistFmServiceClient
    {
        private readonly SetlistApi _setlistApi;
        private readonly ILogger<SetlistFmServiceClient> _logger;

        public SetlistFmServiceClient(ILogger<SetlistFmServiceClient> logger, string token)
        {
            _setlistApi = new SetlistApi(token);
            _logger = logger;
        }

        public async Task<MusicSetlists> SearchArtistSetlists(string artistMBID, int page = 1)
        {
            var result = await _setlistApi.ArtistSetlists(artistMBID, page: page);

            if (result == null || result.Setlist == null || result.Setlist.Count == 0)
            {
                _logger?.LogInformation($"Can't fing setlists for artist with mbid [{artistMBID}]");
                return null;
            }
            return result.ToInternal();
        }

        public async Task<MusicSetlist> SearchSetlist(string setlistId)
        {
            var result = await _setlistApi.Setlist(setlistId);
            if (result == null || result.Sets == null || result.Sets.Set == null || result.Sets.Set.Count == 0)
            {
                _logger?.LogError($"Can't find setlist by Id: [{setlistId}]");
                return null;
            }
            return result.ToInternal();
        }
    }
}
