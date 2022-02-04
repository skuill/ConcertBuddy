using SpotifyAPI.Web;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client.Extensions;

namespace MusicSearcher.Model
{
    public class MusicTrack
    {
        private const string EXTERNAL_URL_KEY = "spotify";

        public FullTrack SpotifyTrack { get; set; }

        public YTrack YandexTrack { get; set; }

        public string TrackName => SpotifyTrack?.Name ?? YandexTrack?.Title;

        public string DownloadLink => YandexTrack?.GetLink();

        public string TrackExternalLink
        {
            get {
                if (SpotifyTrack is null)
                    return null;
                if (SpotifyTrack.ExternalUrls.TryGetValue(EXTERNAL_URL_KEY, out string result))
                    return result;
                return null;
            }
        }

        public string AlbumName
        {
            get
            {
                if (SpotifyTrack != null && SpotifyTrack.Album != null)
                {
                    return SpotifyTrack.Album.Name;
                }
                if (YandexTrack != null && YandexTrack.Albums != null && YandexTrack.Albums.Any())
                {
                    return YandexTrack.Albums.First().Title;
                }
                return null;
            }
        }

        public string AlbumExternalLink
        {
            get
            {
                if (SpotifyTrack != null && SpotifyTrack.Album != null)
                {
                    if (SpotifyTrack.Album.ExternalUrls.TryGetValue(EXTERNAL_URL_KEY, out string result))
                        return result;
                }
                return null;
            }
        }

        public IEnumerable<string> ArtistsNames
        {
            get
            {
                if (SpotifyTrack != null && SpotifyTrack.Artists != null && SpotifyTrack.Artists.Count > 0)
                {
                    return SpotifyTrack.Artists.Select(x => x.Name);
                }
                if (YandexTrack != null && YandexTrack.Artists != null && YandexTrack.Artists.Count > 0)
                {
                    return YandexTrack.Artists.Select(x => x.Name);
                }
                return null;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> ArtistsExternalLinks
        {
            get
            {
                if (SpotifyTrack != null && SpotifyTrack.Artists != null && SpotifyTrack.Artists.Count > 0)
                {
                    return SpotifyTrack.Artists.Where(x => x.ExternalUrls.ContainsKey(EXTERNAL_URL_KEY))
                        .Select(x => KeyValuePair.Create(x.Name, x.ExternalUrls[EXTERNAL_URL_KEY]));
                }    
                return null;
            }
        }

        public TimeSpan Duration => SpotifyTrack != null
            ? TimeSpan.FromMilliseconds(SpotifyTrack.DurationMs)
            : YandexTrack != null
            ? TimeSpan.FromMilliseconds(YandexTrack.DurationMs)
            : TimeSpan.FromMilliseconds(0);
    }
}
