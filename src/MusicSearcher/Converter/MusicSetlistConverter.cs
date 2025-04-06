using MusicSearcher.Model;
using SetlistNet.Models;

namespace MusicSearcher.Converter
{
    internal static class MusicSetlistConverter
    {
        public static MusicSetlist ToInternal(this Setlist setlist)
        {
            return new MusicSetlist
            {
                Id = setlist.Id,
                ArtistMBID = setlist.Artist?.MBID,
                ArtistName = setlist.Artist?.Name,
                CityName = setlist.Venue?.City?.Name,
                CountryName = setlist.Venue?.City?.Country?.Name,
                CountryState = setlist.Venue?.City?.State,
                EventDate = setlist.EventDate.ToString("dd-MM-yyyy"),
                TourName = setlist.Tour?.Name,
                VenueName = setlist.Venue?.Name,
                Sets = setlist.Sets?.ToInternal()
            };
        }
    }
}
