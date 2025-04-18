﻿using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicBrainz;

namespace MusicSearcher
{
    public interface IMusicSearcherClient : IDisposable
    {
        Task<MusicArtistBase> SearchArtistByMBID(string artistMBID);

        Task<MusicArtistBase> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz);

        Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0);

        Task<MusicTrackBase> SearchTrack(string artistName, string trackName);

        /// <summary>
        /// Return TOP tracks from available music services (Spotify,..)
        /// </summary>
        Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID);

        Task<MusicLyric> SearchLyric(string artistName, string songName);

        Task<MusicRecording?> SearchRecordByName(string artistMBID, string recordingName);

        Task<MusicSetlists> SearchArtistSetlists(string artistMBID, int page = 1);

        Task<MusicSetlist> SearchSetlist(string setlistId);
    }
}
