
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// A Result consisting of a list of artists.
    /// </summary>
    public class Artists : ArrayResult<Artist>
    {
        /// <summary>
        /// result list of artists
        /// </summary>
        [JsonPropertyName("artist")]
        public List<Artist> Items { get; set; }

        public bool IsEmpty() => Items == null || !Items.Any();
    }
}
