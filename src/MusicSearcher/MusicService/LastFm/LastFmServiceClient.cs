using IF.Lastfm.Core.Api;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.LastFm;
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

        public MusicServiceType MusicServiceType => MusicServiceType.LastFm;

        public async Task<MusicArtistBase> GetArtistByMBID(string mbid)
        {
            var lastFmArtist = await _lastFmClient.Artist.GetInfoByMbidAsync(mbid);
            if (lastFmArtist != null && lastFmArtist.Success)
            {
                return new LastFmMusicArtist(lastFmArtist.Content);
            }
            else
            {
                throw new Exception($"Can't get artist by mbid [{mbid}] from LastFM. Status: {lastFmArtist?.Status}.");
            }
        }

        public AvailableSearchType GetAvailableSearch() => availableSearch;

        public Task<MusicArtistBase> SearchArtistByName(string name)
        {
            throw new NotImplementedException();
        }

        public Task<List<MusicTrackBase>> SearchTopTracks(MusicArtistBase artist)
        {
            throw new NotImplementedException();
        }

        public Task<MusicTrackBase> SearchTrack(string artistName, string trackName)
        {
            throw new NotImplementedException();
        }
    }
}
