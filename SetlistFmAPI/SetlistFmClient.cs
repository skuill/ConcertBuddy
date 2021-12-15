using Microsoft.Extensions.Logging;
using SetlistFmAPI.Http;
using SetlistFmAPI.Models;

namespace SetlistFmAPI
{
    public class SetlistFmClient: ISetlistFmClient
    {

        private readonly ILogger<SetlistFmClient> _logger;
        private ISetlistHttpClient _httpClient;

        private string _apiKey;
        private string _language = "en";

        public SetlistFmClient(ILogger<SetlistFmClient> logger, ISetlistHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<Artist> SearchArtist(string mbid)
        {
            return await _httpClient.Load<Artist>(SetlistFmUrls.Artist(mbid), _apiKey, _language);
        }

        /// <summary>
        /// Search for artists.
        /// </summary>
        /// <param name="searchFields">
        /// You must provide a value for at least one of the following properties:
        /// <para>MBID, TMID (set <code>TMIDSpecified = true</code>), Name.</para>
        /// </param>
        /// <param name="page">Page number to fetch.</param>
        /// <returns>A list of matching artist.</returns>
        public async Task<Artists> SearchArtists(Artist searchFields, int page = 1)
        {
            return await _httpClient.Load<Artists>(SetlistFmUrls.Artists(searchFields), _apiKey, _language);
        }

        public async Task<Artists> SearchArtists(string artistName, int page = 1)
        {
            return await SearchArtists(new Artist(artistName), page);
        }

        /// <summary>
        /// Get a list of an artist's setlists.
        /// </summary>
        /// <param name="mbid">the Musicbrainz MBID of the artist</param>
        /// <param name="page">the number of the result page</param>
        /// <returns></returns>
        public async Task<Setlists> SearchArtistSetlists(string mbid, int page = 1)
        {
            return await _httpClient.Load<Setlists>(SetlistFmUrls.ArtistSetlists(mbid, page), _apiKey, _language);
        }

        /// <summary>
        /// Returns the current version of a setlist. E.g. if you pass the id of a setlist that got edited since you last accessed it, you'll get the current version.
        /// </summary>
        /// <param name="setlistId">The setlist id.</param>
        /// <returns>The setlist for the provided id.</returns>
        public async Task<Setlist> SearchSetlist(string setlistId)
        {
            return await _httpClient.Load<Setlist>(SetlistFmUrls.Setlist(setlistId), _apiKey, _language);
        }

        public void WithApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public void WithHttpClient(ISetlistHttpClient httpClient)
        {
            if (httpClient != null)
                _httpClient = httpClient;
        }

        public void WithLanguage(string language)
        {
            _language = language;
        }
    }
}