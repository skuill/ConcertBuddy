using MusicSearcher.MusicService;

namespace MusicSearcher.Model.Abstract
{
    public abstract class MusicTrackBase: IMusicTrack
    {
        public abstract MusicServiceType MusicServiceType { get; }
        public abstract string TrackName { get; }
        public abstract string DownloadLink { get; }
        public abstract string TrackExternalLink { get; }
        public abstract string AlbumName { get; }
        public abstract string AlbumExternalLink { get; }
        public abstract IEnumerable<string> ArtistsNames { get; }
        public abstract IEnumerable<KeyValuePair<string, string>> ArtistsExternalLinks { get; }
        public abstract TimeSpan? Duration { get; }

        public abstract MusicTrackBase GetMusicTrackByServiceType(MusicServiceType musicServiceType);

        public abstract bool IsMusicTrackExist(MusicServiceType musicServiceType);
    }
}
