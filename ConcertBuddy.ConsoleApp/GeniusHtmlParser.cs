using HtmlAgilityPack;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConcertBuddy.ConsoleApp
{
    public class GeniusHtmlParser
    {
        private static bool _isStatisWebPage = false;

        private async static Task<string> GetStaticPageLyric(string url)
        {
            var htmlPage = new HtmlWeb();
            var document = htmlPage.Load(url);
            IEnumerable<HtmlNode> nodes =
                document.DocumentNode.Descendants(0).Where(x => x.HasClass("lyrics"));

            string lyrics = nodes.FirstOrDefault().InnerHtml.ToString();

            lyrics = StripTagsRegex(lyrics);
            lyrics = StripNewLines(lyrics);
            lyrics = CleanEnding(lyrics);
            return lyrics;
        }


        //TODO Try to scrape from Musixmatch!
        private async static Task<string> GetDynamicPageLyric(string url)
        {
            ScrapingBrowser browser = new ScrapingBrowser();
            WebPage htmlPage = browser.NavigateToPage(new Uri(url), HttpVerb.Post);

            IEnumerable<HtmlNode> nodes = htmlPage.Html.Descendants(0).Where(x => x.HasClass("lyrics"));

            string lyrics = nodes.FirstOrDefault().InnerHtml.ToString();

            lyrics = StripTagsRegex(lyrics);
            lyrics = StripNewLines(lyrics);
            lyrics = CleanEnding(lyrics);
            return lyrics;
        }

        public async static Task<string> GetLyric(string url)
        {
            if (_isStatisWebPage)
                return await GetStaticPageLyric(url);
            return await GetDynamicPageLyric(url);
        }
        public static string StripTagsRegex(string source)
        {

            return Regex.Replace(source, "<[^>]*>", string.Empty);
        }

        public static string StripNewLines(string source)
        {
            return Regex.Replace(source, @"\t|\n|\r", "</br>");
        }

        public string Urlify(string source)
        {
            return Regex.Replace(source, " ", "%20");
        }

        public static string CleanEnding(string source)
        {
            char[] charsToTrim = { '<', 'b', 'r', '>', ' ', '/' };
            for (int i = 0; i < 20; i++)
            {
                source = source.TrimEnd(charsToTrim);
            }
            return source;
        }
    }
}
