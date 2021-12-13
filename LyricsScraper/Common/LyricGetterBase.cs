using LyricsScraper.Abstract;

namespace LyricsScraper.Common
{
    public abstract class LyricGetterBase: ILyricGetter
    {
        protected IParser Parser { get; set; }
        protected IWebClient WebClient { get; set; }

        public LyricGetterBase()
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
