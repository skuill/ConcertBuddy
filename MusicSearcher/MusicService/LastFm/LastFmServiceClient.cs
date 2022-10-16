using IF.Lastfm.Core.Api;
using MusicSearcher.Model;
using MusicSearcher.MusicService.Abstract;

namespace MusicSearcher.MusicService.LastFm
{
    internal class LastFmServiceClient : IMusicServiceClient
    {
        private LastfmClient _lastFmClient;
        private AvailableSearchType availableSearch = AvailableSearchType.All;

        public LastFmServiceClient(string apiKey, string secret)
        {
            try
            {
                _lastFmClient = new LastfmClient(apiKey, secret);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task GetArtistByMBID(MusicArtist artist, string mbid)
        {
            var lastFmArtist = await _lastFmClient.Artist.GetInfoByMbidAsync(mbid);
            if (lastFmArtist != null && lastFmArtist.Success)
            {
                artist.LastFmArtist = lastFmArtist.Content;
            }
            else
            {
                throw new Exception($"Can't get artist by mbid [{mbid}] from LastFM. Status: {lastFmArtist?.Status}.");
            }
        }

        public AvailableSearchType GetAvailableSearch() => availableSearch;

        public Task SearchArtistByName(MusicArtist artist, string name)
        {
            throw new NotImplementedException();
        }

        public Task<List<MusicTrack>> SearchTopTracks(MusicArtist artist)
        {
            throw new NotImplementedException();
        }

        public Task SearchTrack(MusicTrack track, string artistName, string trackName)
        {
            throw new NotImplementedException();
        }
    }
}
