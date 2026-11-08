using Hqub.Lastfm;
using Hqub.Lastfm.Entities;

namespace MusicSearcher.MusicService.LastFm
{
    public sealed class LastFmApi : ILastFmApi
    {
        private readonly LastfmClient _lastFmClient;

        public LastFmApi(string apiKey, string secret)
        {
            _lastFmClient = new LastfmClient(apiKey, secret);
        }

        public Task<Artist> GetArtistInfoByMbidAsync(string mbid) => _lastFmClient.Artist.GetInfoByMbidAsync(mbid);

        public Task<Artist> GetArtistInfoAsync(string name) => _lastFmClient.Artist.GetInfoAsync(name);

        public async Task<IReadOnlyList<Track>> GetTopTracksByMbidAsync(string mbid, int limit)
        {
            var tracks = await _lastFmClient.Artist.GetTopTracksByMbidAsync(mbid, limit: limit);
            return tracks.ToList();
        }

        public async Task<IReadOnlyList<Track>> GetTopTracksAsync(string artistName, int limit)
        {
            var tracks = await _lastFmClient.Artist.GetTopTracksAsync(artistName, limit: limit);
            return tracks.ToList();
        }
    }
}
