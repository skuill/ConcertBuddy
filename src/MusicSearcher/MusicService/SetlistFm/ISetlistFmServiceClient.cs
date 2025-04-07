using MusicSearcher.Model;

namespace MusicSearcher.MusicService.SetlistFm
{
    public interface ISetlistFmServiceClient
    {
        public Task<MusicSetlists> SearchArtistSetlists(string artistMBID, int page = 1);

        public Task<MusicSetlist> SearchSetlist(string setlistId);
    }
}
