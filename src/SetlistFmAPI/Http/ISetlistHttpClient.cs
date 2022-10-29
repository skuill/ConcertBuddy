namespace SetlistFmAPI.Http
{
    public interface ISetlistHttpClient
    {
        Task<T> Load<T>(Uri url, string apiKey, string language = "en");
    }
}
