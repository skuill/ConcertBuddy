using MusicSearcher.MusicService;

namespace MusicSearcher.Model.Abstract
{
    internal interface IMusicTrack
    {
        public MusicTrackBase? GetMusicTrackByServiceType(MusicServiceType musicServiceType);

        public bool IsMusicTrackExist(MusicServiceType musicServiceType);
    }
}
