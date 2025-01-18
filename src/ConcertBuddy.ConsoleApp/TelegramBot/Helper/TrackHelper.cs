using MusicSearcher.Model.Abstract;
using System.Text;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper
{
    public static class TrackHelper
    {
        public static string GetTrackArtistsLinks(this MusicTrackBase track)
        {
            return string.Join(", ", track.ArtistsExternalLinks!
                .Select(a => $"<a href=\"{a.Value}\">{a.Key}</a>"));
        }

        public static string GetTrackMarkdown(this MusicTrackBase track)
        {
            StringBuilder result = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(track.TrackExternalLink)
                && !string.IsNullOrEmpty(track.TrackName))
                result.AppendLine($"<a href=\"{track.TrackExternalLink}\">{track.TrackName}</a>");
            else if (!string.IsNullOrEmpty(track.TrackName))
                result.AppendLine($"Track: {track.TrackName}");

            if (track.ArtistsExternalLinks != null
                && track.ArtistsExternalLinks.Any())
                result.AppendLine($"Artists: {track.GetTrackArtistsLinks()}");
            else if (track.ArtistsNames != null
                && track.ArtistsNames.Any())
                result.AppendLine($"Artists: {string.Join(", ", track.ArtistsNames)}");

            if (!string.IsNullOrWhiteSpace(track.AlbumName))
            {
                if (!string.IsNullOrWhiteSpace(track.AlbumExternalLink))
                    result.AppendLine($"Album: <a href=\"{track.AlbumExternalLink}\">{track.AlbumName}</a>");
                else
                    result.AppendLine($"Album: {track.AlbumName}");
            }

            result.AppendLine();
            result.AppendLine($"Duration: {track.Duration:m\\:ss}");

            return result.ToString();
        }
    }
}
