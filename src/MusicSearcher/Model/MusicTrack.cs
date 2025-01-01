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

        public override MusicServiceType MusicServiceType => MusicServiceType.None;

        public override string? TrackName
            => GetFirstNonDefaultValue(x => x.TrackName);

        public override string? DownloadLink
            => GetFirstNonDefaultValue(x => x.DownloadLink);

        public override string? TrackExternalLink
            => GetFirstNonDefaultValue(x => x.TrackExternalLink);

        public override string? AlbumName
            => GetFirstNonDefaultValue(x => x.AlbumName);

        public override string? AlbumExternalLink
            => GetFirstNonDefaultValue(x => x.AlbumExternalLink);

        public override IEnumerable<string>? ArtistsNames
            => GetFirstNonDefaultValue(x => x.ArtistsNames);

        public override IEnumerable<KeyValuePair<string, string>>? ArtistsExternalLinks
            => GetFirstNonDefaultValue(x => x.ArtistsExternalLinks);

        public override TimeSpan? Duration
            => GetFirstNonDefaultValue(x => x.Duration);

        private T? GetFirstNonDefaultValue<T>(Func<MusicTrackBase, T> selector)
        {
            return _musicTracks.Select(selector).FirstOrDefault(x => !EqualityComparer<T>.Default.Equals(x, default));
        }

        public void Add(MusicTrackBase track)
        {
            if (!_musicTracks.Contains(track))
                _musicTracks.Add(track);
            else
                throw new InvalidOperationException($"Try to add already exist track {track.MusicServiceType}");
        }

        public override bool IsMusicTrackExist(MusicServiceType musicServiceType)
        {
            return _musicTracks.Any(x => x.MusicServiceType == musicServiceType);
        }

        public override MusicTrackBase? GetMusicTrackByServiceType(MusicServiceType musicServiceType)
        {
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
