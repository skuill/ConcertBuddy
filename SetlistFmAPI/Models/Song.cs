
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// This class represents a song that is part of a <paramref name="Set"/>.
    /// </summary>
    public class Song
    {
        #region Properties
        /// <summary>
        /// The name of the song. E.g. "Yesterday" or "Wish You Were Here".
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// A different <paramref name="Artist"/> than the performing one that joined the stage for this song.
        /// </summary>
        [JsonPropertyName("with")]
        public Artist With { get; set; }

        /// <summary>
        /// The original <paramref name="Artist"/> of this song, if different to the performing artist.
        /// </summary>
        [JsonPropertyName("cover")]
        public Artist Cover { get; set; }

        /// <summary>
        /// Special incidents or additional information about the way the song was performed at this specific concert. See the Guidelines complete list of allowed content.
        /// <para>Guidelines: <see cref="http://www.setlist.fm/guidelines"/>.</para>
        /// </summary>
        [JsonPropertyName("info")]
        public string Info { get; set; }
        #endregion

        /// <summary>
        /// Returns the <paramref name="Name"/> propertu of the object.
        /// </summary>
        /// <returns>A string that represents <paramref name="Name"/> property.</returns>
        public override string ToString()
        {
            return "Name = " + Name;
        }
    }
}
