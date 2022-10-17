using System.Collections.Generic;

using Yandex.Music.Api.Models.Album;

namespace Yandex.Music.Api.Models.Search.Album
{
    public class YSearchAlbumModel: YAlbum
    {
        public List<string> AvailableRegions { get; set; }
        // TODO: Wait for a fix https://github.com/K1llMan/Yandex.Music.Api/issues/6
        public new List<object> Labels { get; set; }
        public int OriginalReleaseYear { get; set; }
        public List<string> Regions { get; set; }
    }
}