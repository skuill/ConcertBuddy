using MusicSearcher.Model;
using SetlistNet.Models;

namespace MusicSearcher.Converter
{
    internal static class MusicSetConverter
    {
        public static MusicSet ToInternal(this Set set)
        {
            return new MusicSet
            {
                Encore = set.Encore,
                Name = set.Name,
                Songs = set.Songs?.Select(s => s.ToInternal()).ToList()
            };
        }
    }
}
