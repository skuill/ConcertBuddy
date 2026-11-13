using MusicSearcher.Model;

namespace MusicSearcher.MusicService.SetlistFm;

public interface ISetlistFmServiceClient
{
    /// <summary>
    /// Null when setlist.fm has nothing (it answers 404).
    /// </summary>
    /// <exception cref="MusicServiceUnavailableException">setlist.fm did not answer.</exception>
    public Task<MusicSetlists?> SearchArtistSetlists(string artistMBID, int page = 1);

    /// <summary>
    /// Null when setlist.fm has nothing (it answers 404).
    /// </summary>
    /// <exception cref="MusicServiceUnavailableException">setlist.fm did not answer.</exception>
    public Task<MusicSetlist?> SearchSetlist(string setlistId);
}
