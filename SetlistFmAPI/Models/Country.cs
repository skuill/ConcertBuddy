
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// This class represents a country on earth.
    /// </summary>
    public class Country
    {
        #region Properties
        /// <summary>
        /// the country's name. Can be a localized name - e.g. "Austria" or "Österreich" for Austria if the German name was requested.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// the country's ISO code. E.g. "ie" for Ireland.
        /// <para>See: <see cref="http://www.iso.org/iso/english_country_names_and_code_elements"/>.</para>
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }
        #endregion

        public Country()
        {

        }

        public Country(string name) : this()
        {
            Name = name;
        }

        public Country(string name, string code)
            : this(name)
        {
            Code = code;
        }

        public override string ToString()
        {
            return string.Format("Name = " + Name);
        }
    }
}
