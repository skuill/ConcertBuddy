﻿using LyricsScraper.Abstract;
using LyricsScraper.Common;
using LyricsScraper.Utils;
using Microsoft.Extensions.Logging;

namespace LyricsScraper.AZLyrics
{
    public class AZLyricsGetter : LyricGetterBase
    {
        private readonly ILogger<AZLyricsGetter> _logger;
        private readonly string _baseUri = "http://www.azlyrics.com/lyrics/";

        private readonly string _lyricStart = "<!-- Usage of azlyrics.com content by any third-party lyrics provider is prohibited by our licensing agreement. Sorry about that. -->";
        private readonly string _lyricEnd = "<!-- MxM banner -->";

        public Uri BaseUri => new Uri(_baseUri);

        public AZLyricsGetter(ILogger<AZLyricsGetter> logger, ILyricParser parser, ILyricWebClient webClient)
        {
            _logger = logger;
            Parser = new AZLyricsParser();
            WebClient = new HtmlAgilityWebClient();
        }

        public override string SearchLyric(string artist, string song)
        {
            return SearchLyric(GetLyricUri(artist, song));
        }

        public override string SearchLyric(Uri uri)
        {
            if (WebClient == null || Parser == null)
            {
                _logger?.LogError($"Please set up WebClient and Parser at first");
                return null;
            }
            var text = WebClient.Load(uri);
            return PostProcessLyric(uri, text);
        }
        public override async Task<string> SearchLyricAsync(string artist, string song)
        {
            return await SearchLyricAsync(GetLyricUri(artist, song));
        }

        public override async Task<string> SearchLyricAsync(Uri uri)
        {
            if (WebClient == null || Parser == null)
            {
                _logger?.LogError($"Please set up WebClient and Parser at first");
                return null;
            }
            var text = await WebClient.LoadAsync(uri);
            return PostProcessLyric(uri, text);
        }

        private Uri GetLyricUri(string artist, string song)
        {
            // http://www.azlyrics.com/lyrics/youngthug/richniggashit.htm
            // remove articles from artist on start. For example for band [The Devil Wears Prada]: https://www.azlyrics.com/d/devilwearsprada.html
            var artistStripped = StringUtils.StripRedundantChars(artist.ToLowerInvariant(), true);
            var titleStripped = StringUtils.StripRedundantChars(song.ToLowerInvariant());
            return new Uri(BaseUri, $"{artistStripped}/{titleStripped}.html");
        }

        private string PostProcessLyric(Uri uri, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _logger?.LogError($"Text is empty for {uri}");
                return null;
            }

            var startIndex = text.IndexOf(_lyricStart);
            var endIndex = text.IndexOf(_lyricEnd);
            if (startIndex <= 0 || endIndex <= 0)
            {
                _logger?.LogError($"Can't find lyrics for {uri}");
                return null;
            }
            return Parser.Parse(text.Substring(startIndex, endIndex - startIndex));
        }
    }
}
