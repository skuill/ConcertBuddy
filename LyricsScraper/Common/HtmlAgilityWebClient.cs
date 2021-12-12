using HtmlAgilityPack;
using LyricsScraper.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.Common
{
    public class HtmlAgilityWebClient : IWebClient
    {
        public string Load(Uri uri)
        {
            var htmlPage = new HtmlWeb();
            var document = htmlPage.Load(uri, "GET");

            return document.ParsedText;
        }
    }
}
