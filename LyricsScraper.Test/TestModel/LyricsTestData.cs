using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LyricsScraper.Test.TestModel
{
    public class LyricsTestData
    {
        public string LyricPagePath { get; set; }
        public string LyricResultPath { get; set; }
        public string ArtistName { get; set; }
        public string SongName { get; set; }
        public string SongUri { get; set; }

        public string LyricPageData => File.ReadAllText(LyricPagePath);

        public string LyricResultData => File.ReadAllText(LyricResultPath);
    }
}
