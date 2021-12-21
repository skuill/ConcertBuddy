
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// A Result consisting of a list of setlists.
    /// </summary>
    public class Setlists : ArrayResult<Setlist>
    {
        /// <summary>
        /// the list of setlists
        /// </summary>
        [JsonPropertyName("setlist")]
        public List<Setlist> Items { get; set; }

        public bool IsEmpty() => Items == null || !Items.Any();

        public override string ToString()
        {
            return string.Format("Count = {0}", Items == null ? 0 : Items.Count);
        }
    }
}
