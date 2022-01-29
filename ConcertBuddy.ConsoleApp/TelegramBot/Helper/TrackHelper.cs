using MusicSearcher.Model;
using System.Text;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper
{
    public static class TrackHelper
    {
        public static string GetTrackArtistsLinks(this MusicTrack track)
        {
            return string.Join(", ", track.SpotifyTrack.Artists
                .Select(artist => $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>"));
        }

        public static string GetTrackMarkdown(this MusicTrack track)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{track.SpotifyTrack.ExternalUrls["spotify"]}\">{track.Name}</a>")
                .AppendLine($"Artists: {track.GetTrackArtistsLinks()}")
                .AppendLine($"Album: <a href=\"{track.SpotifyTrack.Album.ExternalUrls["spotify"]}\">{track.SpotifyTrack.Album.Name}</a>")
                .AppendLine($"Duration: {TimeSpan.FromMilliseconds(track.SpotifyTrack.DurationMs):m\\:ss}")
                .ToString();
        }
    }
}
