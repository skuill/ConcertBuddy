
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// This class represents a city where Venues are located. 
    /// Most of the original city data was taken from <see cref="http://geonames.org"/>.
    /// </summary>
    public class City
    {
        #region Properties
        /// <summary>
        /// unique identifier.
        /// <para>This is <code>geoNameId</code> property in <see cref="http://geonames.org"/>.
        /// Setlist.fm API returns <code>cu:aa1a96e1-06c7-11e6-b736-22000bb3106b</code> for <code>id=3d59d97</code>, so switching to int temporarily.</para>
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// the city's name, depending on the language valid values are e.g. "Mchen" or "Munich".
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// the name of city's state, e.g. "Bavaria" or "Florida".
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; }

        /// <summary>
        /// the code of the city's state. For most countries this two-digit numeric code, 
        /// with which the state can be identified uniquely in the specific Country. See remarks for more info.
        /// <para>E.g. "CA" or 02.</para>
        /// </summary>
        /// <remarks>
        /// Valid examples are "CA" or "02" which in turn get uniquely identifiable 
        /// when combined with the state's country:
        /// 
        /// "US.CA" for California, United States or
        /// "DE.02" for Bavaria, Germany
        /// 
        /// For a complete list of available states (that aren't necessarily used in this database) 
        /// is available in <see cref="http://download.geonames.org/export/dump/admin1Codes.txt"/> a textfile on geonames.org.
        /// 
        /// Note that this code is only unique combined with the city's Country. The code alone is not unique.
        /// </remarks>
        [JsonPropertyName("stateCode")]
        public string StateCode { get; set; }

        /// <summary>
        /// the city's coordinates. Usually the coordinates of the city centre are used.
        /// </summary>
        [JsonPropertyName("coords")]
        public Coords Coords { get; set; }

        /// <summary>
        /// the city's country.
        /// </summary>
        [JsonPropertyName("country")]
        public Country Country { get; set; }
        #endregion

        public City()
        {

        }

        public City(int geoNamesId)
            : this()
        {
            Id = geoNamesId.ToString();
        }

        public City(string name)
            : this()
        {
            Name = name;
        }

        public City(Country country)
            : this()
        {
            Country = country;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(State))
                return string.Format("{0} ({1})", Name, Country.Name);
            else
                return string.Format("{0}, {1} ({2})", Name, State, Country.Name);
        }
    }
}
