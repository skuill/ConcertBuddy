using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicBrainz;

namespace MusicSearcher;

public interface IMusicSearcherClient : IDisposable
{
    /// <summary>
    /// The artist from MusicBrainz with what the other services know about it; an empty artist (no name) when MusicBrainz has none.
    /// </summary>
    /// <exception cref="MusicServiceUnavailableException">MusicBrainz did not answer.</exception>
    Task<MusicArtistBase> SearchArtistByMBID(string artistMBID);

    Task<MusicArtistBase> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz);

    /// <summary>
    /// Artists of MusicBrainz for the name, best first; empty when nothing is found.
    /// </summary>
    /// <exception cref="MusicServiceUnavailableException">MusicBrainz did not answer.</exception>
    Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0);

    Task<MusicTrackBase> SearchTrack(string artistName, string trackName);

    /// <summary>
    /// Return TOP tracks from available music services (Last.fm)
    /// </summary>
    Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID);

    Task<MusicLyric> SearchLyric(string artistName, string songName);

    Task<MusicRecording?> SearchRecordByName(string artistMBID, string recordingName);

    /// <summary>
    /// Null when setlist.fm has nothing (it answers 404).
    /// </summary>
    /// <exception cref="MusicServiceUnavailableException">setlist.fm did not answer.</exception>
    Task<MusicSetlists?> SearchArtistSetlists(string artistMBID, int page = 1);

    /// <summary>
    /// Null when setlist.fm has nothing (it answers 404).
    /// </summary>
    /// <exception cref="MusicServiceUnavailableException">setlist.fm did not answer.</exception>
    Task<MusicSetlist?> SearchSetlist(string setlistId);
}
