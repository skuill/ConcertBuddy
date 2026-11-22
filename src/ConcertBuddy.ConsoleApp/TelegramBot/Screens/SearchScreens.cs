using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using MusicSearcher;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// The artist search: "Searching…", the list of artists with pages, nothing found, service unavailable.
/// </summary>
public sealed class SearchScreens
{
    /// <summary>
    /// Longer texts are cut: no artist name is that long, and the text goes into the screen and the buttons.
    /// </summary>
    public const int MaxSearchTextLength = 100;

    public const int PageSize = SearchConstants.SEARCH_ARTISTS_LIMIT_DEFAULT;

    /// <summary>
    /// Artists that tour now, so their setlists are fresh (checked on setlist.fm in #46).
    /// </summary>
    public static readonly IReadOnlyList<string> Examples = new[] { "Metallica", "Linkin Park", "Foo Fighters", "Ed Sheeran" };

    /// <summary>
    /// Under a list or a card that came from Last.fm, so the missing country, type and years do not look like a bug.
    /// </summary>
    public const string LastFmNote = "MusicBrainz is busy, so this comes from Last.fm.";

    private readonly IMusicSearcherClient _musicSearcher;
    private readonly CallbackCodec _codec;
    private readonly ArtistScreens _artistScreens;

    public SearchScreens(IMusicSearcherClient musicSearcher, CallbackCodec codec, ArtistScreens artistScreens)
    {
        _musicSearcher = musicSearcher;
        _codec = codec;
        _artistScreens = artistScreens;
    }

    public static string CleanText(string text)
    {
        return ScreenText.Shorten(text.Trim(), MaxSearchTextLength);
    }

    public static Screen Searching(string text)
    {
        return new Screen($"🔍 Searching for <b>{ScreenText.Html(text)}</b>…", InlineKeyboardMarkup.Empty());
    }

    /// <summary>
    /// A page of the search. A new search (not a page turn or the way back from a card) opens the card at once when one
    /// artist has exactly the typed name, and tries the other keyboard layout when nothing is found (#94).
    /// </summary>
    public async Task<ScreenResult> SearchAsync(string text, int offset, bool newSearch = false)
    {
        List<MusicArtistBase> artists;
        try
        {
            artists = (await _musicSearcher.SearchArtistsByName(text, limit: PageSize, offset: offset)).ToList();
        }
        catch (MusicServiceUnavailableException exception)
        {
            return CommonScreens.Unavailable(exception.Service, _codec.Encode(CallbackActions.NewSearch, Number(offset), text));
        }

        if (artists.Count > 0)
        {
            var match = newSearch && offset == 0 ? ConfidentMatch(text, artists) : null;
            return match != null
                ? await _artistScreens.CardAsync(match.MBID!, 0, text, confidentMatch: true)
                : new ScreenResult(List(text, artists, offset), CommandResult.Found);
        }

        if (offset > 0)
        {
            return new ScreenResult(NoMorePages(text, offset), CommandResult.NotFound);
        }

        return newSearch ? await OtherLayoutAsync(text) : new ScreenResult(NotFound(text), CommandResult.NotFound);
    }

    /// <summary>
    /// The one artist whose name is the typed text (letters and digits, any case), or null when there is none or several:
    /// "Nirvana" has two bands, so the list stays.
    /// </summary>
    private static MusicArtistBase? ConfidentMatch(string text, List<MusicArtistBase> artists)
    {
        var typed = Letters(text);
        var matches = artists.Where(artist => !string.IsNullOrEmpty(artist.MBID) && Letters(artist.Name ?? string.Empty) == typed).ToList();
        return typed.Length > 0 && matches.Count == 1 ? matches[0] : null;
    }

    private static string Letters(string text)
    {
        return string.Concat(text.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    /// <summary>
    /// Nothing found: the same text typed on the other keyboard layout, "ьуефддшсф" for "metallica". The list says what was
    /// searched instead, so the result is no surprise.
    /// </summary>
    private async Task<ScreenResult> OtherLayoutAsync(string text)
    {
        var swapped = KeyboardLayout.Swap(text);
        if (swapped != text)
        {
            List<MusicArtistBase> artists;
            try
            {
                artists = (await _musicSearcher.SearchArtistsByName(swapped, limit: PageSize, offset: 0)).ToList();
            }
            catch (MusicServiceUnavailableException exception)
            {
                return CommonScreens.Unavailable(exception.Service, _codec.Encode(CallbackActions.NewSearch, Number(0), text));
            }

            if (artists.Count > 0)
            {
                var title = $"🤔 Nothing found for <b>{ScreenText.Html(text)}</b>. Did you mean <b>{ScreenText.Html(swapped)}</b>?";
                return new ScreenResult(List(swapped, artists, 0, title), CommandResult.Found);
            }
        }

        return new ScreenResult(NotFound(text), CommandResult.NotFound);
    }

    private Screen List(string text, List<MusicArtistBase> artists, int offset, string? title = null)
    {
        var rows = artists
            .Select(artist => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{ScreenText.Flag(artist.Country)} {artist.Name} · {ScreenText.Describe(artist)}".TrimEnd(' ', '·'),
                    _codec.Encode(CallbackActions.Artist, artist.MBID ?? string.Empty, Number(offset), text)),
            })
            .ToList();
        rows.Add(Navigation(text, offset, hasNextPage: artists.Count == PageSize));

        var page = offset == 0 ? string.Empty : $" (page {(offset / PageSize) + 1})";
        var html = title ?? $"Choose the artist for <b>{ScreenText.Html(text)}</b>{page}:";
        // MusicBrainz did not answer and the list came from Last.fm (#91): without country, type and years.
        if (!artists[0].IsMusicArtistExist(MusicServiceType.MusicBrainz))
        {
            html += $"\n<i>{LastFmNote}</i>";
        }
        return new Screen(html, new InlineKeyboardMarkup(rows));
    }

    private Screen NotFound(string text)
    {
        var html = $"🤷 Nothing found for <b>{ScreenText.Html(text)}</b>.\n\n"
            + "• Check the spelling\n"
            + "• Send the name of the band, not of a song\n"
            + "• Try the original spelling (Кино, not Kino)\n\n"
            + "Or try one of these:";
        var examples = Examples
            .Select(name => InlineKeyboardButton.WithCallbackData(name, NewSearchData(name)))
            .Chunk(2)
            .Append(new[] { CommonScreens.DeleteButton() });
        return new Screen(html, new InlineKeyboardMarkup(examples));
    }

    private Screen NoMorePages(string text, int offset)
    {
        var html = $"No more artists for <b>{ScreenText.Html(text)}</b>.";
        return new Screen(html, new InlineKeyboardMarkup(new[] { Navigation(text, offset, hasNextPage: false) }));
    }

    private InlineKeyboardButton[] Navigation(string text, int offset, bool hasNextPage)
    {
        var buttons = new List<InlineKeyboardButton>();
        if (offset > 0)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("⬅️", SearchData(_codec, text, Math.Max(0, offset - PageSize))));
        }

        buttons.Add(CommonScreens.DeleteButton());
        if (hasNextPage)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("➡️", SearchData(_codec, text, offset + PageSize)));
        }

        return buttons.ToArray();
    }

    public static string SearchData(CallbackCodec codec, string text, int offset)
    {
        return codec.Encode(CallbackActions.Search, Number(offset), text);
    }

    private string NewSearchData(string text)
    {
        return _codec.Encode(CallbackActions.NewSearch, Number(0), text);
    }

    private static string Number(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
