using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using MusicSearcher;
using MusicSearcher.Model.Abstract;
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

    private readonly IMusicSearcherClient _musicSearcher;
    private readonly CallbackCodec _codec;

    public SearchScreens(IMusicSearcherClient musicSearcher, CallbackCodec codec)
    {
        _musicSearcher = musicSearcher;
        _codec = codec;
    }

    public static string CleanText(string text)
    {
        return ScreenText.Shorten(text.Trim(), MaxSearchTextLength);
    }

    public static Screen Searching(string text)
    {
        return new Screen($"🔍 Searching for <b>{ScreenText.Html(text)}</b>…", InlineKeyboardMarkup.Empty());
    }

    public async Task<ScreenResult> SearchAsync(string text, int offset)
    {
        List<MusicArtistBase> artists;
        try
        {
            artists = (await _musicSearcher.SearchArtistsByName(text, limit: PageSize, offset: offset)).ToList();
        }
        catch (MusicServiceUnavailableException exception)
        {
            return CommonScreens.Unavailable(exception.Service, SearchData(text, offset));
        }

        if (artists.Count > 0)
        {
            return new ScreenResult(List(text, artists, offset), CommandResult.Found);
        }

        return offset == 0
            ? new ScreenResult(NotFound(text), CommandResult.NotFound)
            : new ScreenResult(NoMorePages(text, offset), CommandResult.NotFound);
    }

    private Screen List(string text, List<MusicArtistBase> artists, int offset)
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
        return new Screen($"Choose the artist for <b>{ScreenText.Html(text)}</b>{page}:", new InlineKeyboardMarkup(rows));
    }

    private Screen NotFound(string text)
    {
        var html = $"🤷 Nothing found for <b>{ScreenText.Html(text)}</b>.\n\n"
            + "• Check the spelling\n"
            + "• Send the name of the band, not of a song\n"
            + "• Try the original spelling (Кино, not Kino)\n\n"
            + "Or try one of these:";
        var examples = Examples
            .Select(name => InlineKeyboardButton.WithCallbackData(name, SearchData(name, 0)))
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
            buttons.Add(InlineKeyboardButton.WithCallbackData("⬅️", SearchData(text, Math.Max(0, offset - PageSize))));
        }

        buttons.Add(CommonScreens.DeleteButton());
        if (hasNextPage)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("➡️", SearchData(text, offset + PageSize)));
        }

        return buttons.ToArray();
    }

    public string SearchData(string text, int offset)
    {
        return _codec.Encode(CallbackActions.Search, Number(offset), text);
    }

    private static string Number(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
