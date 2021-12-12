using HtmlAgilityPack;
using LyricsScraper.Abstract;
using LyricsScraper.Common;
using LyricsScraper.Unils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.AZLyrics
{
    public class AZLyricsGetter : LyricGetter
    {
        private readonly ILogger<AZLyricsGetter> _logger;
        private readonly Uri _baseUri;

        private readonly string _lyricStart = "<!-- Usage of azlyrics.com content by any third-party lyrics provider is prohibited by our licensing agreement. Sorry about that. -->";
        private readonly string _lyricEnd = "<!-- MxM banner -->";

        public AZLyricsGetter(string endpoint = "http://www.azlyrics.com/lyrics/")
            : this(new Uri(endpoint)) { }

        public AZLyricsGetter(Uri endpoint): base()
        {
            _baseUri = endpoint;
            Parser = new AZLyricsParser();
            WebClient = new HtmlAgilityWebClient();
        }

        public override string SearchLyric(string artist, string song)
        {
            // http://www.azlyrics.com/lyrics/youngthug/richniggashit.htm
            var artistStripped = StringUtils.StripRedundantChars(artist.ToLowerInvariant());
            var titleStripped = StringUtils.StripRedundantChars(song.ToLowerInvariant());

            return SearchLyric(new Uri(_baseUri, $"{artistStripped}/{titleStripped}.html"));
        }

        public override string SearchLyric(Uri uri)
        {
            var text = WebClient.Load(uri);
            if (string.IsNullOrEmpty(text))
            {
                _logger?.LogError($"text is empty for {uri}");
                return null;
            }

            var startIndex = text.IndexOf(_lyricStart);
            var endIndex = text.IndexOf(_lyricEnd);
            if (startIndex <= 0 || endIndex <= 0)
            {
                _logger?.LogError($"can't find lyrics for {uri}");
                return null;
            }
            return Parser.Parse(text.Substring(startIndex, endIndex - startIndex));
        }
    }
}
