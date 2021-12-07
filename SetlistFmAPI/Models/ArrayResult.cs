
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// This is an abstract class, that represents a set of items, returned by API
    /// </summary>
    public class ArrayResult<T>
    {
        /// <summary>
        /// the total amount of items matching the query
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// the current page. starts at 1
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// the amount of items you get per page
        /// </summary>
        [JsonPropertyName("itemsPerPage")]
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// the property "type" of an object.
        /// </summary>
        [JsonPropertyName("type")]
        public string ApiType { get; set; }
    }
}
