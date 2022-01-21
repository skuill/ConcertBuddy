using Genius;
using HtmlAgilityPack;
using LyricsScraper.Abstract;
using LyricsScraper.Common;
using Microsoft.Extensions.Logging;

namespace LyricsScraper.Genius
{
    public class GeniusGetter : LyricGetterBase
    {
        private readonly ILogger<GeniusGetter> _logger;
        private readonly string _apiKey; 

        public GeniusGetter(ILogger<GeniusGetter> logger, ILyricParser parser, ILyricWebClient webClient, string apiKey)
        {
            _logger = logger;
            _apiKey = apiKey;
            Parser = new GeniusParser();
            WebClient = new HtmlAgilityWebClient();
        }

        // TODO: add parsing for dynamic site. Try to use Selenium webdriver scraping c#
        // scrapysharp not working. Return 503 error. https://github.com/rflechner/ScrapySharp
        [Obsolete("Do not call this method.")]
        public override string SearchLyric(Uri uri)
        {
            throw new NotImplementedException();

            var htmlPage = new HtmlWeb();
            var document = htmlPage.Load(uri, "GET");
            IEnumerable<HtmlNode> nodes =
                document.DocumentNode.Descendants(0).Where(x => x.Attributes.Any(a => a.Name == "data-lyrics-container" && a.Value == "true"));

            string lyric = nodes.FirstOrDefault().InnerHtml.ToString();

            return Parser.Parse(lyric);
        }

        // TODO: add parsing for dynamic site. Try to use Selenium webdriver scraping c#
        // scrapysharp not working. Return 503 error. https://github.com/rflechner/ScrapySharp
        [Obsolete("Do not call this method.")]
        public override string SearchLyric(string artist, string song)
        {
            throw new NotImplementedException();

            var artistAndSong = $"{artist} {song}";

            var geniusClient = new GeniusClient(_apiKey);
            var searchGeniusResult = geniusClient.SearchClient.Search(artistAndSong).GetAwaiter().GetResult();
            if (searchGeniusResult.Meta.Status != 200)
            {
                _logger.LogError($"Can't find any information about artist {artist} and song {song}. Code: {searchGeniusResult.Meta.Status}. Message: {searchGeniusResult.Meta.Message}");
                return null;
            }
            var artistAndSongHit = searchGeniusResult.Response.Hits.FirstOrDefault(x => string.Equals(x.Result.PrimaryArtist.Name, artist, StringComparison.OrdinalIgnoreCase));

            if (artistAndSongHit == null || artistAndSongHit.Result == null)
            {
                _logger.LogError($"Can't find artist {artist} and song {song} hit.");
                return null;
            }

            _logger.LogDebug($"Genius artist and song url: {artistAndSongHit.Result.Url}");

            // https://genius.com/Parkway-drive-wishing-wells-lyrics
            var lyricUrl = artistAndSongHit.Result.Url;

            return SearchLyric(new Uri(lyricUrl));
        }

        public override Task<string> SearchLyricAsync(Uri uri)
        {
            throw new NotImplementedException();
        }

        public override Task<string> SearchLyricAsync(string artist, string song)
        {
            throw new NotImplementedException();
        }
    }
}
