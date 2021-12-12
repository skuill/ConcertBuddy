using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SetlistFmAPI.Http
{
    public class HttpSetlistWebClient: IHttpClient
    {
        private readonly string _language;
        private readonly string _apiKey;

        private readonly ILogger<HttpSetlistWebClient> _logger;

        public HttpSetlistWebClient(string apiKey, string language = "en")
        {
            _language = language;
            _apiKey = apiKey;
        }

        public async Task<T> Load<T>(Uri url)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = SetlistFmUrls.APIV1;
            httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            httpClient.DefaultRequestHeaders.Add("Accept-Language", _language);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>();

            _logger?.LogError($"Response status: {response.StatusCode}. {response.ReasonPhrase}. Url: {url}");
            return default(T);
        }
    }
}
