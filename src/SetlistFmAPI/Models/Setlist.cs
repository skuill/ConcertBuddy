
using System.Globalization;
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// This class represents a setlist. 
    /// <para>
    /// Please remember that setlist.fm is a wiki, so there are different versions of the same setlist. 
    /// Thus, the best way to check whether two setlists are the same is to use <paramref name="VersioId"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// A setlist can be distinguished from other setlists by its unique id. But as setlist.fm works the wiki way, there can
    /// be different versions of one setlist (each time a user updates a setlist a new version gets created). 
    /// These different versions have a unique id on its own. So setlists can have the same id although they differ as far 
    /// as the content is concerned - thus the best way to check if two setlists are the same is to compare their versionIds.
    /// </remarks>
    public class Setlist
    {
        #region Properties
        /// <summary>
        /// the setlist's artist.
        /// </summary>
        [JsonPropertyName("artist")]
        public Artist Artist { get; set; }

        /// <summary>
        /// the setlist's venue.
        /// </summary>
        [JsonPropertyName("venue")]
        public Venue Venue { get; set; }

        /// <summary>
        /// the tour in which the band performed setlist.
        /// </summary>
        [JsonPropertyName("tour")]
        public Tour Tour { get; set; }

        [JsonIgnore]
        public string TourName
        {
            get
            {
                if (Tour == null)
                    return null;
                return Tour.Name;
            }
            set
            {
                if (Tour == null)
                    Tour = new Tour();
                Tour.Name = value;
            }
        }

        /// <summary>
        /// all sets of this setlist.
        /// </summary>
        [JsonPropertyName("sets")]
        public Sets Sets { get; set; }

        /// <summary>
        /// additional information on the concert - see the Guidelines for a complete list of allowed content.
        /// <para>See: <see cref="http://www.setlist.fm/guidelines"/>.</para>
        /// </summary>
        [JsonPropertyName("info")]
        public string Info { get; set; }

        /// <summary>
        /// the attribution url to which you have to link to wherever you use data from this setlist in your application.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// unique identifier of setlist.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// unique identifier of the version of setlist.
        /// </summary>
        [JsonPropertyName("versionId")]
        public string VersionId { get; set; }

        /// <summary>
        /// date of the concert in the format "dd-MM-yyyy", e.g. 31-03-2007.
        /// </summary>
        [JsonPropertyName("eventDate")]
        public string EventDate { get; set; }

        /// <summary>
        /// date, time and time zone of the last update to this setlist in the format "yyyy-MM-dd'T'HH:mm:ss.SSSZZZZZ".
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public string LastUpdated { get; set; }
        #endregion

        public Setlist()
        {

        }

        public Setlist(Artist artist)
            : this()
        {
            Artist = artist;
        }

        public DateTime? GetEventDateTime()
        {
            DateTime result;
            if (DateTime.TryParseExact(EventDate, "dd-MM-yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }
            
            return null;
        }

        public string GetEventDateTime(string format)
        {
            var dt = GetEventDateTime();
            return dt.HasValue ? dt.Value.ToString(format) : "";
        }

        public string GetEventDateTime(string format, IFormatProvider provider)
        {
            var dt = GetEventDateTime();
            return dt.HasValue ? dt.Value.ToString(format, provider) : "";
        }

        public void SetEventDateTime(DateTime dt)
        {
            EventDate = dt.ToString("dd-MM-yyyy");
        }

        public ushort GetYear()
        {
            if (EventDate != null && EventDate.Length == 10)
            {
                return Convert.ToUInt16(EventDate.Substring(6));
            }
            else
                return 0;
        }

        public bool IsSetsExist()
        {
            return Sets != null && Sets.Items != null && Sets.Items.Any();
        }

        public override string ToString()
        {
            string result = $"[{EventDate}]";
            if (!string.IsNullOrEmpty(TourName))
                result += $"({TourName})";
            result += $" {Artist.Name} @";
            if (!string.IsNullOrEmpty(Venue.Name))
                result += $" {Venue.Name}";

            var cityInfo = Venue.City.ToString();
            if (!string.IsNullOrEmpty(cityInfo))
                result += $"\r\nat {cityInfo}";

            return $"[{EventDate}]({TourName}) {Artist.Name} @ {Venue.Name} {Venue.City.ToString()}";
        }
    }
}
