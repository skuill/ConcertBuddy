using HtmlAgilityPack;
using LyricsScraper.Abstract;

namespace LyricsScraper.Common
{
    public class HtmlAgilityWebClient : ILyricWebClient
    {
        public string Load(Uri uri)
        {
            var htmlPage = new HtmlWeb();
            var document = htmlPage.Load(uri, "GET");

            return document?.ParsedText;
        }

        public async Task<string> LoadAsync(Uri uri)
        {
            var htmlPage = new HtmlWeb();
            var document = await htmlPage.LoadFromWebAsync(uri.ToString());

            return document?.ParsedText;
        }
    }
}
