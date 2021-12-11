using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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
        private static bool _isStatisWebPage = true;

        private async static Task<string> GetStaticPageLyric(string url)
        {
            var htmlPage = new HtmlWeb();
            var document = htmlPage.Load(url, "GET");
            IEnumerable<HtmlNode> nodes =
                document.DocumentNode.Descendants(0).Where(x => x.Attributes.Any(a => a.Name == "data-lyrics-container" && a.Value == "true"));

            string lyrics = nodes.FirstOrDefault().InnerHtml.ToString();

            lyrics = StripTagsRegex(lyrics);
            lyrics = StripNewLines(lyrics);
            lyrics = CleanEnding(lyrics);
            return lyrics;
        }


        //TODO Try to scrape from Musixmatch!
        private async static Task<string> GetDynamicPageLyric(string url)
        {
            string lyrics = String.Empty;
            try
            {
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArguments("headless");
                IWebDriver driver = new ChromeDriver(chromeOptions);

                driver.Navigate().GoToUrl(url);

                IWebElement lyricElement = driver.FindElement(By.Id("lyrics"));
                lyrics = lyricElement.Text;

                lyrics = StripTagsRegex(lyrics);
                lyrics = StripNewLines(lyrics);
                lyrics = CleanEnding(lyrics);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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
