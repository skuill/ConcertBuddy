
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// A Result consisting of a list of cities.
    /// </summary>
    public class Cities : ArrayResult<City>
    {
        /// <summary>
        /// the list of cities.
        /// </summary>
        [JsonPropertyName("cities")]
        public List<City> Items { get; set; }

        public override string ToString()
        {
            return string.Format("Count = {0}", Items == null ? 0 : Items.Count);
        }
    }
}
