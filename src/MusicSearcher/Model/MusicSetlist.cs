using System.Text;

namespace MusicSearcher.Model
{
    public class MusicSetlist
    {
        public string? Id { get; set; }

        public string? EventDate { get; set; }

        public string? TourName { get; set; }

        public string? ArtistMBID { get; set; }

        public string? ArtistName { get; set; }

        public string? VenueName { get; set; }

        public string? CountryName { get; set; }

        public string? CountryState { get; set; }

        public string? CityName { get; set; }

        public MusicSets? Sets { get; set; }

        public bool IsSetsExist()
        {
            return Sets != null && Sets.Set != null && Sets.Set.Any();
        }

        private string GetPlaceString()
        {
            if (string.IsNullOrEmpty(CountryState))
                return string.Format("{0} ({1})", CityName, CountryName);
            else
                return string.Format("{0}, {1} ({2})", CityName, CountryState, CountryName);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder($"[{EventDate}]");

            if (!string.IsNullOrEmpty(TourName))
                result.Append($"({TourName})");
            result.Append($" {ArtistName} @");
            if (!string.IsNullOrEmpty(VenueName))
                result.Append($" {VenueName}");

            var cityInfo = GetPlaceString();
            if (!string.IsNullOrEmpty(cityInfo))
                result.Append($" {cityInfo}");

            return result.ToString();
        }
    }
}
