using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.Abstract
{
    public interface IParser
    {
        string Parse(string lyric);
    }
}
