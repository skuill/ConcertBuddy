namespace SetlistFmAPI.Http
{
    public interface IHttpClient
    {
        Task<T> Load<T>(Uri url, string apiKey, string language = "en");
    }
}
