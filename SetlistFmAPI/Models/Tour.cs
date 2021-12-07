
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// The tour a setlist was a part of.
    /// </summary>
    public class Tour
    {
        /// <summary>
        /// The name of the tour. e.g. "North American Tour 1964".
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public Tour()
        {
        }

        public Tour(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return "Name = " + Name;
        }
    }
}
