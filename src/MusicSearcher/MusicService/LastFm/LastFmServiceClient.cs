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
            _lastFmClient = new LastfmClient(apiKey, secret);
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
            catch (ServiceException ex)
            {
                throw new Exception($"Hqub.Lastfm returns error with code [{ex.ErrorCode}] and message [{ex.Message}]");
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while getting artist by MBID [{mbid}].", ex);
            }
        }

        public AvailableSearchType GetAvailableSearch() => availableSearch;

        public async Task<MusicArtistBase> SearchArtistByName(string name)
        {
            try
            {
                var lastFmArtist = await _lastFmClient.Artist.GetInfoAsync(name);

                if (lastFmArtist != null)
                {
                    return new LastFmMusicArtist(lastFmArtist);
                }
                else
                {
                    throw new Exception($"Artist with name [{name}] was not found on LastFM.");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Network error while fetching artist by name [{name}] from LastFM.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Request to LastFM for name [{name}] timed out.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while getting artist by name [{name}].", ex);
            }
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
