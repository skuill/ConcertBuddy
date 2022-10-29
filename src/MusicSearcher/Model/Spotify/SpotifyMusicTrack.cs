using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;
using SpotifyAPI.Web;

namespace MusicSearcher.Model.Spotify
{
    public class SpotifyMusicTrack : MusicTrackBase
    {
        private const string EXTERNAL_URL_KEY = "spotify";
        private FullTrack _track { get; set; }

        public SpotifyMusicTrack(FullTrack track)
        {
            _track = track;
        }
        public override string TrackName => _track.Name;

        public override string DownloadLink => default;

        public override string TrackExternalLink => _track.ExternalUrls.TryGetValue(EXTERNAL_URL_KEY, out string result)
            ? result
            : default;

        public override string AlbumName => _track.Album != null
            ? _track.Album.Name
            : default;

        public override string AlbumExternalLink => _track.Album != null && _track.Album.ExternalUrls.TryGetValue(EXTERNAL_URL_KEY, out string result)
            ? result
            : default;

        public override IEnumerable<string> ArtistsNames => _track.Artists != null && _track.Artists.Count > 0
            ? _track.Artists.Select(x => x.Name)
            : default;

        public override IEnumerable<KeyValuePair<string, string>> ArtistsExternalLinks => _track.Artists != null && _track.Artists.Count > 0
            ? _track.Artists.Where(x => x.ExternalUrls.ContainsKey(EXTERNAL_URL_KEY))
                        .Select(x => KeyValuePair.Create(x.Name, x.ExternalUrls[EXTERNAL_URL_KEY]))
            : default;

        public override TimeSpan? Duration => TimeSpan.FromMilliseconds(_track.DurationMs);

        public override MusicServiceType MusicServiceType => MusicServiceType.Spotify;

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
