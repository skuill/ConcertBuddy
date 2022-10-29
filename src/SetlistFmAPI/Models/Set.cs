
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// A setlist consists of different (at least one) sets. Sets can either be sets as defined in the Guidelines or encores.
    /// <para>See: <see cref="http://www.setlist.fm/guidelines"/>.</para>
    /// </summary>
    public class Set
    {
        #region Private Fields
        private int? _encore;
        private bool _encoreSpecified;
        #endregion

        #region Properties
        /// <summary>
        /// If the set is an encore, this property the number of the encore, starting with 1 for the first encore, 2 for the second and so on.
        /// </summary>
        [JsonPropertyName("encore")]
        public int Encore
        {
            get
            {
                return this._encore.GetValueOrDefault();
            }
            set
            {
                this._encore = value;
                this._encoreSpecified = true;
            }
        }

        /// <summary>
        /// whether the "Encore" property should be included in the output.
        /// </summary>
        public bool EncoreSpecified
        {
            get
            {
                return this._encoreSpecified;
            }
            set
            {
                this._encoreSpecified = value;
            }
        }

        /// <summary>
        /// the description/name of the set. E.g. "Acoustic set" or "Paul McCartney solo".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// this set's songs.
        /// </summary>
        [JsonPropertyName("song")]
        public List<Song> Songs { get; set; }
        #endregion

        public override string ToString()
        {
            string result = "";
            if (EncoreSpecified)
                result = "[Encore " + Encore + "] ";
            else
                result = "Set ";
            
            return $"{result}{Name}. Songs = {Songs.Count}";
        }
    }
}
