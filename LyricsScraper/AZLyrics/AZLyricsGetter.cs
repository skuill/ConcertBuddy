using HtmlAgilityPack;
using LyricsScraper.Abstract;
using LyricsScraper.Unils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.AZLyrics
{
    public class AZLyricsGetter : IGetter
    {
        private IParser _parser;

        private readonly ILogger<AZLyricsGetter> _logger;
        private readonly Uri _baseUri;

        private readonly string _lyricStart = "<!-- Usage of azlyrics.com content by any third-party lyrics provider is prohibited by our licensing agreement. Sorry about that. -->";
        private readonly string _lyricEnd = "<!-- MxM banner -->";

        public AZLyricsGetter(string endpoint = "http://www.azlyrics.com/lyrics/")
            : this(new Uri(endpoint)) { }

        public AZLyricsGetter(Uri endpoint)
        {
            _baseUri = endpoint;
            _parser = new AZLyricsParser();
        }

        public string SearchLyric(string artist, string song)
        {
            // http://www.azlyrics.com/lyrics/youngthug/richniggashit.htm
            var artistStripped = StringUtils.StripWhiteSpaces(artist.ToLowerInvariant());
            var titleStripped = StringUtils.StripWhiteSpaces(song.ToLowerInvariant());

            //_uri = new Uri(_url + artist + "/" + title + ".html", UriKind.Absolute);

            return SearchLyric(new Uri(_baseUri, $"{artistStripped}/{titleStripped}.html"));
        }

        public string SearchLyric(Uri uri)
        {
            var htmlPage = new HtmlWeb();
            var document = htmlPage.Load(uri, "GET");

            var text = document.ParsedText;
            if (string.IsNullOrEmpty(text))
            {
                _logger?.LogError($"text is empty for {uri}");
            }

            var startIndex = text.IndexOf(_lyricStart);
            var endIndex = text.IndexOf(_lyricEnd);
            return _parser.Parse(text.Substring(startIndex, endIndex - startIndex));
        }

        public void WithParser(IParser parser)
        {
            if (parser != null)
                _parser = parser;
        }
    }
}
