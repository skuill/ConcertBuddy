using MusicSearcher.MusicService;

namespace MusicSearcher.Model.Abstract
{
    internal interface IMusicArtist
    {
        public MusicArtistBase? GetMusicArtistByServiceType(MusicServiceType musicServiceType);

        public bool IsMusicArtistExist(MusicServiceType musicServiceType);
    }
}
