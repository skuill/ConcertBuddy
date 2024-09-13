using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace SetlistFmAPI.Http
{
    public class SetlistHttpWebClient : ISetlistHttpClient
    {
        private readonly ILogger<SetlistHttpWebClient> _logger;

        public SetlistHttpWebClient(ILogger<SetlistHttpWebClient> logger)
        {
            _logger = logger;
        }

        public async Task<T> Load<T>(Uri url, string apiKey, string language = "en")
        {
            int retryCount = 1;
            int retryAmount = 3;

            do
            {
                try
                {
                    var httpClient = new HttpClient();
                    httpClient.BaseAddress = SetlistFmUrls.APIV1;
                    httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                    httpClient.DefaultRequestHeaders.Add("Accept-Language", language);
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                    var response = await httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogError($"Try [{retryCount}] of [{retryAmount}] to load setlist error. Response status: {response.StatusCode}. {response.ReasonPhrase}. Url: {url}");
                        continue;
                    }
                    return await response.Content.ReadFromJsonAsync<T>();
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Try №{retryCount} to load setlist error: {ex}. Url: {url}");
                    Thread.Sleep(1000);
                }
            } while (retryCount++ <= retryAmount);

            return default(T);
        }
    }
}
