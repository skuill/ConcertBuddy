
using System.Text.Json.Serialization;

namespace SetlistFmAPI.Models
{
    /// <summary>
    /// This class represents an artist. An artist is a musician or a group of musicians. 
    /// Each artist has a definite Musicbrainz Identifier (MBID) with which the artist can be uniquely identified.
    /// <para>See <seealso cref="http://wiki.musicbrainz.org/MBID"/> for more info about Musicbrainz ID.</para>
    /// </summary>
    public class Artist
    {
        #region Properties
        /// <summary>
        /// unique Musicbrainz Identifier (MBID), e.g. "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d"
        /// </summary>
        [JsonPropertyName("mbid")]
        public string MBID { get; set; }

        /// <summary>
        /// unique Ticket Master Identifier (TMID), e.g. 735610
        /// </summary>
        [JsonPropertyName("tmid")]
        public string TMID { get; set; }

        /// <summary>
        /// the artist's name, e.g. "The Beatles"
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// the artist's sort name, e.g. "Beatles, The" or "Springsteen, Bruce"
        /// </summary>
        [JsonPropertyName("sortName")]
        public string SortName { get; set; }

        /// <summary>
        /// disambiguation to distinguish between artists with same names
        /// </summary>
        [JsonPropertyName("disambiguation")]
        public string Disambiguation { get; set; }

        /// <summary>
        /// the attribution url
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }
        #endregion

        public string NameWithDisambiguation
        {
            get
            {
                if (string.IsNullOrEmpty(Disambiguation))
                    return Name;
                else
                    return Name + " (" + Disambiguation + ")";
            }
        }

        public Artist()
        {
        }

        public Artist(string name)
            : this()
        {
            Name = name;
        }
    }
}
