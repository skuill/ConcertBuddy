﻿using Hqub.MusicBrainz.API.Entities;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicBrainz;

namespace MusicSearcher.Abstract
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

        Task<Recording> SearchSongByName(string artistMBID, string name);

        Task<Recording> SearchSongByMBID(string songMBID);

        void WithLastFmClient(string apiKey, string secret);

        Task WithSpotifyClient(string cliendID, string clientSecret);

        Task WithYandexClient(string token);

        void WithMemoryCache();
    }
}
