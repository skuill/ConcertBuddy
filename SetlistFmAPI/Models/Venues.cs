
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// A Result consisting of a list of venues.
    /// </summary>
    public class Venues : ArrayResult<Venue>
    {
        /// <summary>
        /// the list of venues.
        /// </summary>
        [JsonPropertyName("venue")]
        public List<Venue> Items { get; set; }

        public override string ToString()
        {
            return string.Format("Count = {0}", Items == null ? 0 : Items.Count);
        }
    }
}
