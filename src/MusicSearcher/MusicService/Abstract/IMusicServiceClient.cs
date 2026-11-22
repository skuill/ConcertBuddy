using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;

namespace MusicSearcher.MusicService.Abstract;

public interface IMusicServiceClient
{
    public MusicServiceType MusicServiceType { get; }

    public Task<MusicArtistBase> GetArtistByMBID(string mbid);

    public Task<MusicArtistBase> SearchArtistByName(string name);

    /// <summary>
    /// Artists for the name, best first: the search when MusicBrainz does not answer (#91).
    /// </summary>
    public Task<IReadOnlyList<MusicArtistBase>> SearchArtistsByName(string name, int limit, int offset);

    /// <summary>
    /// Null when the service has no such track: a normal result, not a failure.
    /// </summary>
    public Task<MusicTrackBase?> SearchTrack(string artistName, string trackName);

    public Task<List<MusicTrackBase>> SearchTopTracks(MusicArtistBase artist);

    public AvailableSearchType GetAvailableSearch();
}
