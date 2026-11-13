using MusicSearcher.Model.Abstract;
using System.Net;
using System.Text.RegularExpressions;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// Pieces of screen text shared by the screens.
/// </summary>
public static partial class ScreenText
{
    /// <summary>
    /// Escapes only what Telegram HTML needs; WebUtility.HtmlEncode would also turn "·" and "ö" into numeric entities.
    /// </summary>
    public static string Html(string? text)
    {
        return (text ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    /// <summary>
    /// Cuts at a word boundary and adds "…", so a long text keeps the message under the Telegram limits.
    /// </summary>
    public static string Shorten(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        var cut = text[..maxLength];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > maxLength / 2 ? cut[..lastSpace] : cut).TrimEnd() + "…";
    }

    /// <summary>
    /// The flag of a two-letter country code, a microphone for anything else.
    /// </summary>
    public static string Flag(string? country)
    {
        if (country is not { Length: 2 } || !country.All(char.IsAsciiLetter))
        {
            return "🎤";
        }

        return string.Concat(country.ToUpperInvariant().Select(letter => char.ConvertFromUtf32(0x1F1E6 + letter - 'A')));
    }

    /// <summary>
    /// "Group · United States · 1981 - ", what tells apart artists with the same name.
    /// </summary>
    public static string Describe(MusicArtistBase artist)
    {
        var years = artist.ActiveYears?.Trim(' ', '-');
        var parts = new[] { artist.Type, artist.Area, string.IsNullOrEmpty(years) ? null : artist.ActiveYears!.Trim() };
        return string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    /// <summary>
    /// The Last.fm biography as plain text: no tags, no "Read more on Last.fm" link.
    /// </summary>
    public static string Biography(string? biography)
    {
        if (string.IsNullOrWhiteSpace(biography))
        {
            return string.Empty;
        }

        var text = WebUtility.HtmlDecode(HtmlTag().Replace(biography, string.Empty));
        return text.Replace("Read more on Last.fm", string.Empty).Trim();
    }

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex HtmlTag();
}
