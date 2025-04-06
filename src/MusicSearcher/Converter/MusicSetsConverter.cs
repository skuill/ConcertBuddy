using MusicSearcher.Model;
using SetlistNet.Models;

namespace MusicSearcher.Converter
{
    internal static class MusicSetsConverter
    {
        public static MusicSets ToInternal(this Sets sets)
        {
            return new MusicSets
            {
                Set = sets.Set?.Select(s => s.ToInternal()).ToList()
            };
        }
    }
}
