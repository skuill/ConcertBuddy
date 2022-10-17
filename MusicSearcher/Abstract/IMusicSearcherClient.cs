﻿using Hqub.MusicBrainz.API.Entities;
using MusicSearcher.Model;
using MusicSearcher.MusicBrainz;

namespace MusicSearcher.Abstract
{
    public interface IMusicSearcherClient : IDisposable
    {
        Task<MusicArtist> SearchArtistByMBID(string artistMBID);

        Task<MusicArtist> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz);

        Task<IEnumerable<MusicArtist>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0);

        Task<MusicTrack> SearchTrack(string artistName, string trackName);

        /// <summary>
        /// Return TOP tracks from available music services (Spotify,..)
        /// </summary>
        Task<IEnumerable<MusicTrack>> SearchTopTracks(string artistMBID);

        Task<Recording> SearchSongByName(string artistMBID, string name);

        Task<Recording> SearchSongByMBID(string songMBID);

        void WithLastFmClient(string apiKey, string secret);

        Task WithSpotifyClient(string cliendID, string clientSecret);

        Task WithYandexClient(string token);

        void WithMemoryCache();
    }
}
