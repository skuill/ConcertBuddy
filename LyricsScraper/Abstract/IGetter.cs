using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.Abstract
{
    public interface IGetter
    {
        string SearchLyric(Uri uri);

        string SearchLyric(string artist, string song);

        void WithParser(IParser parser);
    }
}
