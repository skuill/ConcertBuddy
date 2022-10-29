using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client.Extensions;

namespace MusicSearcher.Model.Yandex
{
    public class YandexMusicTrack : MusicTrackBase
    {
        private YTrack _track { get; set; }

        public YandexMusicTrack(YTrack track)
        {
            _track = track;
        }
        public override string TrackName => _track.Title;

        public override string DownloadLink => _track.GetLink();

        public override string TrackExternalLink => default;

        public override string AlbumName => _track.Albums != null && _track.Albums.Any()
            ? _track.Albums.First().Title
            : default;

        public override string AlbumExternalLink => default;

        public override IEnumerable<string> ArtistsNames => _track.Artists != null && _track.Artists.Count > 0
            ? _track.Artists.Select(x => x.Name)
            : default;

        public override IEnumerable<KeyValuePair<string, string>> ArtistsExternalLinks => default;

        public override TimeSpan? Duration => TimeSpan.FromMilliseconds(_track.DurationMs);

        public override MusicServiceType MusicServiceType => MusicServiceType.Yandex;

        public override MusicTrackBase GetMusicTrackByServiceType(MusicServiceType musicServiceType)
        {
            if (MusicServiceType == musicServiceType)
                return this;
            return null;
        }

        public override bool IsMusicTrackExist(MusicServiceType musicServiceType)
        {
            return MusicServiceType == musicServiceType;
        }
    }
}
