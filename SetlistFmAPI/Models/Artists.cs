using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// A Result consisting of a list of artists.
    /// </summary>
    public class Artists : ArrayResult<Artist>
    {
        /// <summary>
        /// Gets or sets the list of artists.
        /// </summary>
        [JsonPropertyName("artist")]
        public List<Artist> Items { get; set; }
    }
}
