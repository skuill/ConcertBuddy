using SetlistFmAPI.Models;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace SetlistFmAPI
{
    public class SetlistFmClient: ISetlistFmClient
    {
        private readonly string _language;
        private readonly string _apiKey;
        private static string root = "https://api.setlist.fm";
        private static string apiVersion = "1.0";

        private readonly ILogger<SetlistFmClient> _logger;

        public SetlistFmClient(string apiKey)
        {
            _language = "en";
            _apiKey = apiKey;
        }

        public SetlistFmClient(string apiKey, string language)
            : this(apiKey)
        {
            _language = language;
        }

        public async Task<Artist> SearchArtist(string mbid)
        {
            string url = string.Format("/artist/{0}", mbid);
            Artist artist = await Load<Artist>(url);

            return artist;
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
            StringBuilder query = new StringBuilder();
            if (searchFields != null)
            {
                if (!string.IsNullOrEmpty(searchFields.MBID))
                    query.AppendFormat("artistMbid={0}&", searchFields.MBID);
                if (searchFields.TMIDSpecified)
                    query.AppendFormat("artistTmid={0}&", searchFields.TMID);
                if (!string.IsNullOrEmpty(searchFields.Name))
                    query.AppendFormat("artistName={0}&", searchFields.Name);
            }

            string url = string.Format("/search/artists?{0}sort=relevance&p={1}", query.ToString(), page.ToString());
            Artists artists = await Load<Artists>(url);

            return artists;
        }

        public async Task<Artists> SearchArtists(string artistName, int page = 1)
        {
            return await SearchArtists(new Artist(artistName), page);
        }

        private async Task<T> Load<T>(string url)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(root);
            httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            httpClient.DefaultRequestHeaders.Add("Accept-Language", _language);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await httpClient.GetAsync($"/rest/{apiVersion}{url}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>();

            _logger?.LogError($"Response status: {response.StatusCode}. {response.ReasonPhrase}");
            return default(T);
        }
    }
}