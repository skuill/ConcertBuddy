using LyricsScraper.Abstract;
using LyricsScraper.Unils;

namespace LyricsScraper.AZLyrics
{
    public class AZLyricsParser : ILyricParser
    {
        public string Parse(string lyric)
        {
            return RemoveAllHtmlTags(lyric);
        }

        private string RemoveAllHtmlTags(string html)
        {
            html = StringUtils.RemoveHtmlTags(html);

            // fix recursive white-spaces
            while (html.Contains("  "))
            {
                html = html.Replace("  ", " ");
            }

            // fix recursive line-break
            while (html.Contains("\r\n\r\n\r\n"))
            {
                html = html.Replace("\r\n\r\n\r\n", "\r\n\r\n");
            }

            return html;
        }
    }
}
