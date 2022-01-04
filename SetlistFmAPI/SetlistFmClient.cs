using Microsoft.Extensions.Logging;
using SetlistFmAPI.Http;
using SetlistFmAPI.Models;

namespace SetlistFmAPI
{
    public class SetlistFmClient: ISetlistFmClient
    {

        private readonly ILogger<SetlistFmClient> _logger;
        private ISetlistHttpClient _httpClient;

        private string _apiKey = string.Empty;
        private string _language = "en";

        public SetlistFmClient(ILogger<SetlistFmClient> logger, ISetlistHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<Artist> SearchArtist(string mbid)
        {
            return await _httpClient.Load<Artist>(SetlistFmUrls.Artist(mbid), _apiKey, _language);
        }

        /// <inheritdoc />
        public async Task<Artists> SearchArtists(Artist searchFields, int page = 1)
        {
            return await _httpClient.Load<Artists>(SetlistFmUrls.Artists(searchFields, page:page), _apiKey, _language);
        }

        /// <inheritdoc />
        public async Task<Artists> SearchArtists(string artistName, int page = 1)
        {
            return await SearchArtists(new Artist(artistName), page);
        }

        /// <inheritdoc />
        public async Task<Setlists> SearchArtistSetlists(string mbid, int page = 1)
        {
            return await _httpClient.Load<Setlists>(SetlistFmUrls.ArtistSetlists(mbid, page), _apiKey, _language);
        }

        /// <inheritdoc />
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