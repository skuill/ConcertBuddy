using SetlistFmAPI.Models;
using System.Text;

namespace SetlistFmAPI
{
    public static class SetlistFmUrls
    {
        public static readonly Uri APIV1 = new("https://api.setlist.fm/rest/1.0/");

        public static Uri Artist(string mbid) => RelativeUri($"artist/{mbid}");

        public static Uri Artists(Artist searchFields, string sort = "relevance", int page = 1)
        {
            StringBuilder query = new StringBuilder();
            if (searchFields != null)
            {
                if (!string.IsNullOrEmpty(searchFields.MBID))
                    query.AppendFormat("artistMbid={0}&", searchFields.MBID);
                if (!string.IsNullOrEmpty(searchFields.TMID))
                    query.AppendFormat("artistTmid={0}&", searchFields.TMID);
                if (!string.IsNullOrEmpty(searchFields.Name))
                    query.AppendFormat("artistName={0}&", searchFields.Name);
            }

            return RelativeUri($"search/artists?{query}sort={sort}&p={page}");
        }

        public static Uri ArtistSetlists(string mbid, int page = 1) => RelativeUri($"artist/{mbid}/setlists?p={page}");

        public static Uri Setlist(string setlistId) => RelativeUri($"setlist/{setlistId}");

        public static Uri RelativeUri(FormattableString path) => new(path.ToString().TrimStart('/'), UriKind.Relative);
    }
}
