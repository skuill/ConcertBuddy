using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;
using System.Collections;

namespace MusicSearcher.Model
{
    public class MusicTrack : MusicTrackBase, IEnumerable<MusicTrackBase>
    {
        private List<MusicTrackBase> _musicTracks;

        // Indexer with only a get accessor with the expression-bodied definition:
        public MusicTrackBase this[MusicServiceType musicServiceType] => GetMusicTrackByServiceType(musicServiceType);

        public MusicTrack()
        {
            _musicTracks = new List<MusicTrackBase>();
        }

        public MusicTrack(MusicTrackBase musicTrack)
        {
            _musicTracks = new List<MusicTrackBase> { musicTrack };
        }

        public MusicTrack(List<MusicTrackBase> musicTracks)
        {
            _musicTracks = musicTracks;
        }

        public override string TrackName { get => _musicTracks?.Select(x => x.TrackName).Where(x => x != default).FirstOrDefault(); }

        public override string DownloadLink { get => _musicTracks?.Select(x => x.DownloadLink).Where(x => x != default).FirstOrDefault(); }

        public override string TrackExternalLink { get => _musicTracks?.Select(x => x.TrackExternalLink).Where(x => x != default).FirstOrDefault(); }

        public override string AlbumName { get => _musicTracks?.Select(x => x.AlbumName).Where(x => x != default).FirstOrDefault(); }

        public override string AlbumExternalLink { get => _musicTracks?.Select(x => x.AlbumExternalLink).Where(x => x != default).FirstOrDefault(); }

        public override IEnumerable<string> ArtistsNames { get => _musicTracks?.Select(x => x.ArtistsNames).Where(x => x != default).FirstOrDefault(); }

        public override IEnumerable<KeyValuePair<string, string>> ArtistsExternalLinks { get => _musicTracks?.Select(x => x.ArtistsExternalLinks).Where(x => x != default).FirstOrDefault(); }

        public override TimeSpan? Duration { get => _musicTracks?.Select(x => x.Duration).Where(x => x != default)?.First(); }

        public override MusicServiceType MusicServiceType => MusicServiceType.None;

        public void Add(MusicTrackBase track)
        {
            if (!_musicTracks.Contains(track))
                _musicTracks.Add(track);
            else
                throw new InvalidOperationException($"Try to add already exist track {track.MusicServiceType}");
        }

        public override bool IsMusicTrackExist(MusicServiceType musicServiceType)
        {
            return _musicTracks != null && _musicTracks.Any(x => x.MusicServiceType == musicServiceType);
        }

        public override MusicTrackBase GetMusicTrackByServiceType(MusicServiceType musicServiceType)
        {
            if (_musicTracks == null)
                return null;

            return _musicTracks.FirstOrDefault(x => x.MusicServiceType == musicServiceType);
        }

        public IEnumerator<MusicTrackBase> GetEnumerator()
        {
            return _musicTracks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
