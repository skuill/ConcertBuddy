using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;

namespace MusicSearcher.MusicService.Abstract
{
    internal interface IMusicServiceClient
    {
        public MusicServiceType MusicServiceType { get; }

        public Task<MusicArtistBase> GetArtistByMBID(string mbid);

        public Task<MusicArtistBase> SearchArtistByName(string name);

        public Task SearchTrack(MusicTrack track, string artistName, string trackName);

        public Task<List<MusicTrack>> SearchTopTracks(MusicArtistBase artist);

        public AvailableSearchType GetAvailableSearch();
    }
}
