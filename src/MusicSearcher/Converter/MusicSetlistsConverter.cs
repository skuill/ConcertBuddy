using MusicSearcher.Model;
using SetlistNet.Models.ArrayResult;

namespace MusicSearcher.Converter
{
    internal static class MusicSetlistsConverter
    {
        public static MusicSetlists ToInternal(this Setlists setlists)
        {
            return new MusicSetlists
            {
                ItemsPerPage = setlists.ItemsPerPage,
                Page = setlists.Page,
                Total = setlists.Total,
                Setlist = setlists.Setlist?.Select(s => s.ToInternal()).ToList()
            };
        }
    }
}
