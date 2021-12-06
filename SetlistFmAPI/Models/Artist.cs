using System.Net.Http;
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
        /// <summary>
        /// Disambiguation to distinguish between artists with same names.
        /// </summary>
        [JsonPropertyName("disambiguation")]
        public string Disambiguation { get; set; }

        /// <summary>
        /// Gets or sets unique Musicbrainz Identifier (MBID), e.g. "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d" (The Beatles).
        /// </summary>
        [JsonPropertyName("mbid")]
        public string MBID { get; set; }

        /// <summary>
        /// Gets or sets unique Ticket Master Identifier (TMID), e.g. 1953.
        /// </summary>
        [JsonPropertyName("tmid")]
        public int TMID { get; set; }

        /// <summary>
        /// Gets or sets the <paramref name="TMID"/> property should be included in the output.
        /// </summary>
        /// 
        public bool TMIDSpecified { get; set; }

        /// <summary>
        /// Gets or sets the artist's name, e.g. "The Beatles" or "Bruce Springsteen".
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the artist's sort name, e.g. "Beatles, The" or "Springsteen, Bruce".
        /// </summary>
        [JsonPropertyName("sortName")]
        public string SortName { get; set; }

        /// <summary>
        /// Gets or sets the url to artist's setlists' page on Setlist.fm.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets the url to artist's stats' page on Setlist.fm.
        /// </summary>
        public string UrlStats
        {
            get
            {
                return Url.Replace("/setlists/", "/stats/");
            }
        }

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

        public override string ToString()
        {
            return "Name = " + Name;
        }
    }
}
