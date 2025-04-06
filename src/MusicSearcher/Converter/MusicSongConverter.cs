using MusicSearcher.Model;
using SetlistNet.Models;

namespace MusicSearcher.Converter
{
    internal static class MusicSongConverter
    {
        public static MusicSong ToInternal(this Song song)
        {
            return new MusicSong
            {
                Name = song.Name,
                CoverName = song.Cover?.Name,
                Info = song.Info,
                WithName = song.With?.Name,
            };
        }
    }
}
