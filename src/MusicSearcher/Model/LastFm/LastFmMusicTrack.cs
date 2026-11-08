using Hqub.Lastfm.Entities;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;

namespace MusicSearcher.Model.LastFm
{
    public class LastFmMusicTrack : MusicTrackBase
    {
        private Track _track { get; set; }

        public LastFmMusicTrack(Track track)
        {
            _track = track;
        }

        public override MusicServiceType MusicServiceType => MusicServiceType.LastFm;

        public override string? TrackName => _track.Name;

        public override string? DownloadLink => default;

        public override string? TrackExternalLink => _track.Url;

        public override string? AlbumName => default;

        public override string? AlbumExternalLink => default;

        public override IEnumerable<string>? ArtistsNames => !string.IsNullOrWhiteSpace(_track.Artist?.Name)
            ? new[] { _track.Artist.Name }
            : default;

        public override IEnumerable<KeyValuePair<string, string>>? ArtistsExternalLinks => default;

        public override TimeSpan? Duration => default;
    }
}
