using LyricsScraper.Abstract;

namespace LyricsScraper.Common
{
    public abstract class LyricGetterBase: ILyricGetter
    {
        protected ILyricParser Parser { get; set; }
        protected ILyricWebClient WebClient { get; set; }

        public LyricGetterBase()
        {
        }

        public abstract string SearchLyric(Uri uri);

        public abstract string SearchLyric(string artist, string song);

        public void WithParser(ILyricParser parser)
        {
            if (parser != null)
                Parser = parser;
        }

        public void WithWebClient(ILyricWebClient webClient)
        {
            if (webClient != null)
                WebClient = webClient;
        }
    }
}
