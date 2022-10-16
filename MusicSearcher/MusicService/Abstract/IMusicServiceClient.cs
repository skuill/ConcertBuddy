using MusicSearcher.Model;

namespace MusicSearcher.MusicService.Abstract
{
    internal interface IMusicServiceClient
    {
        public Task GetArtistByMBID(MusicArtist artist, string mbid);

        public Task SearchArtistByName(MusicArtist artist, string name);

        public Task SearchTrack(MusicTrack track, string artistName, string trackName);

        public Task<List<MusicTrack>> SearchTopTracks(MusicArtist artist);

        public AvailableSearchType GetAvailableSearch();
    }
}
