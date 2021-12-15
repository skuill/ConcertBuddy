using LyricsScraper.Abstract;
using System.Text.RegularExpressions;

namespace LyricsScraper.Genius
{
    internal class GeniusParser: ILyricParser
    {
        public string Parse(string lyric)
        {
            lyric = StripTagsRegex(lyric);
            lyric = StripNewLines(lyric);
            lyric = CleanEnding(lyric);
            return lyric;
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
