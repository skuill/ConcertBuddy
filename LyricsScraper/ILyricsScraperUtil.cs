using LyricsScraper.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper
{
    public interface ILyricsScraperUtil
    {
        string SearchLyric(Uri uri);

        string SearchLyric(string artist, string song);

        void AddGetter(ILyricGetter getter);
    }
}
