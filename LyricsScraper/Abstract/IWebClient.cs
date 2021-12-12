using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.Abstract
{
    public interface IWebClient
    {
        string Load(Uri uri);
    }
}
