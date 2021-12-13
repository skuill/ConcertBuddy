using SetlistFmAPI.Http;
using SetlistFmAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetlistFmAPI
{
    public interface ISetlistFmClient
    {
        public Task<Artist> SearchArtist(string mbid);

        public Task<Artists> SearchArtists(Artist searchFields, int page = 1);

        public Task<Artists> SearchArtists(string artistName, int page = 1);

        public Task<Setlists> SearchArtistSetlists(string mbid, int page = 1);

        public Task<Setlist> SearchSetlist(string setlistId);

        public void WithHttpClient(IHttpClient httpClient);

        public void WithApiKey(string apiKey);

        public void WithLanguage(string language);
    }
}
