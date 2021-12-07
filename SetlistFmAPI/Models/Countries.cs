
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// A Result consisting of a list of countries.
    /// </summary>
    public class Countries : ArrayResult<Country>
    {
        /// <summary>
        /// the list of countries.
        /// </summary>
        [JsonPropertyName("country")]
        public List<Country> Items { get; set; }

        public override string ToString()
        {
            return string.Format("Count = {0}", Items == null ? 0 : Items.Count);
        }
    }
}
