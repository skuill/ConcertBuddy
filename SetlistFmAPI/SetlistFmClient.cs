using SetlistFmAPI.Models;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SetlistFmAPI.Http;

namespace SetlistFmAPI
{
    public class SetlistFmClient: ISetlistFmClient
    {

        private readonly ILogger<SetlistFmClient> _logger;
        private IHttpClient _httpClient;

        public SetlistFmClient(string apiKey, string language = "en")
        {
            _httpClient = new HttpSetlistWebClient(apiKey, language);
        }

        public async Task<Artist> SearchArtist(string mbid)
        {
            return await _httpClient.Load<Artist>(SetlistFmUrls.Artist(mbid));
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
            return await _httpClient.Load<Artists>(SetlistFmUrls.Artists(searchFields));
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
            return await _httpClient.Load<Setlists>(SetlistFmUrls.ArtistSetlists(mbid, page));
        }

        /// <summary>
        /// Returns the current version of a setlist. E.g. if you pass the id of a setlist that got edited since you last accessed it, you'll get the current version.
        /// </summary>
        /// <param name="setlistId">The setlist id.</param>
        /// <returns>The setlist for the provided id.</returns>
        public async Task<Setlist> SearchSetlist(string setlistId)
        {
            return await _httpClient.Load<Setlist>(SetlistFmUrls.Setlist(setlistId));
        }

        public void WithHttpClient(IHttpClient httpClient)
        {
            if (httpClient != null)
                _httpClient = httpClient;
        }
    }
}