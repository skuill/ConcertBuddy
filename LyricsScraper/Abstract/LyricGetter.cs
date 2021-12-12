using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.Abstract
{
    public abstract class LyricGetter
    {
        protected IParser Parser { get; set; }
        protected IWebClient WebClient { get; set; }

        public LyricGetter()
        {
        }

        public abstract string SearchLyric(Uri uri);

        public abstract string SearchLyric(string artist, string song);

        public void WithParser(IParser parser)
        {
            if (parser != null)
                Parser = parser;
        }

        public void WithWebClient(IWebClient webClient)
        {
            if (webClient != null)
                WebClient = webClient;
        }
    }
}
