
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// a list of sets is setlist
    /// </summary>
    public class Sets
    {
        [JsonPropertyName("set")]
        public List<Set> Items { get; set; }
    }
}
