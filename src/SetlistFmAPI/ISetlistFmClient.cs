using SetlistFmAPI.Http;
using SetlistFmAPI.Models;

namespace SetlistFmAPI
{
    public interface ISetlistFmClient
    {
        public Task<Artist> SearchArtist(string mbid);

        /// <summary>
        /// Search for artists.
        /// </summary>
        /// <param name="searchFields">
        /// You must provide a value for at least one of the following properties:
        /// <para>MBID, TMID (set <code>TMIDSpecified = true</code>), Name.</para>
        /// </param>
        /// <param name="page">Page number to fetch.</param>
        /// <returns>A list of matching artist.</returns>
        public Task<Artists> SearchArtists(Artist searchFields, int page = 1);

        public Task<Artists> SearchArtists(string artistName, int page = 1);

        /// <summary>
        /// Get a list of an artist's setlists.
        /// </summary>
        /// <param name="mbid">the Musicbrainz MBID of the artist</param>
        /// <param name="page">the number of the result page</param>
        /// <returns></returns>
        public Task<Setlists> SearchArtistSetlists(string mbid, int page = 1);

        /// <summary>
        /// Returns the current version of a setlist. E.g. if you pass the id of a setlist that got edited since you last accessed it, you'll get the current version.
        /// </summary>
        /// <param name="setlistId">The setlist id.</param>
        /// <returns>The setlist for the provided id.</returns>
        public Task<Setlist> SearchSetlist(string setlistId);

        public void WithHttpClient(ISetlistHttpClient httpClient);

        public void WithApiKey(string apiKey);

        public void WithLanguage(string language);
    }
}
