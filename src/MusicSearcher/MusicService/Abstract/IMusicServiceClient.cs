using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;

namespace MusicSearcher.MusicService.Abstract;

public interface IMusicServiceClient
{
    public MusicServiceType MusicServiceType { get; }

    public Task<MusicArtistBase> GetArtistByMBID(string mbid);

    public Task<MusicArtistBase> SearchArtistByName(string name);

    /// <summary>
    /// Null when the service has no such track: a normal result, not a failure.
    /// </summary>
    public Task<MusicTrackBase?> SearchTrack(string artistName, string trackName);

    public Task<List<MusicTrackBase>> SearchTopTracks(MusicArtistBase artist);

    public AvailableSearchType GetAvailableSearch();
}
