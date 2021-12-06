using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// This is an abstract class, that represents a set of items, returned by API
    /// </summary>
    public class ArrayResult<T>
    {
        /// <summary>
        /// Gets or sets the total amount of items matching the query.
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets current page.
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the amount of items you get per page.
        /// </summary>
        [JsonPropertyName("itemsPerPage")]
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// Gets or sets the property "type" of an object.
        /// </summary>
        [JsonPropertyName("type")]
        public string ApiType { get; set; }
    }
}
