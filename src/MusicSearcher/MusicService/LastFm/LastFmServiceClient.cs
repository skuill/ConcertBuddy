using Hqub.Lastfm;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.LastFm;
using MusicSearcher.MusicService.Abstract;

namespace MusicSearcher.MusicService.LastFm
{
    public sealed class LastFmServiceClient : IMusicServiceClient
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
            try
            {
                var lastFmArtist = await _lastFmClient.Artist.GetInfoByMbidAsync(mbid);

                if (lastFmArtist != null)
                {
                    return new LastFmMusicArtist(lastFmArtist);
                }
                else
                {
                    throw new Exception($"Artist with MBID [{mbid}] was not found on LastFM.");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error while fetching artist by MBID [{mbid}] from LastFM.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Request to LastFM for MBID [{mbid}] timed out.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while getting artist by MBID [{mbid}].", ex);
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
