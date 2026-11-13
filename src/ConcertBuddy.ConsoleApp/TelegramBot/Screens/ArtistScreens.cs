using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Microsoft.Extensions.Options;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.MusicService;
using System.Globalization;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// The artist card: name, what the artist is, a short biography, the Last.fm page as the preview with the photo.
/// </summary>
public sealed class ArtistScreens
{
    /// <summary>
    /// The card keeps a short biography; the full one has its own button.
    /// </summary>
    public const int MaxBiographyLength = 600;

    private readonly IMusicSearcherClient _musicSearcher;
    private readonly CallbackCodec _codec;
    private readonly SearchScreens _searchScreens;
    private readonly Configuration _configuration;

    public ArtistScreens(IMusicSearcherClient musicSearcher, CallbackCodec codec, SearchScreens searchScreens, IOptions<Configuration> configuration)
    {
        _musicSearcher = musicSearcher;
        _codec = codec;
        _searchScreens = searchScreens;
        _configuration = configuration.Value;
    }

    /// <summary>
    /// The card of the artist; the search text and offset lead back to the results (an empty text: no search, no button).
    /// </summary>
    public async Task<ScreenResult> CardAsync(string mbid, int searchOffset, string searchText)
    {
        MusicArtist artist;
        try
        {
            artist = (MusicArtist)await _musicSearcher.SearchArtistByMBID(mbid);
        }
        catch (MusicServiceUnavailableException exception)
        {
            var retry = _codec.Encode(CallbackActions.Artist, mbid, searchOffset.ToString(CultureInfo.InvariantCulture), searchText);
            return CommonScreens.Unavailable(exception.Service, retry);
        }

        if (string.IsNullOrEmpty(artist.Name))
        {
            var html = "🤷 MusicBrainz does not have this artist any more. Try the search again.";
            return new ScreenResult(new Screen(html, new InlineKeyboardMarkup(new[] { LastRow(searchOffset, searchText) })), CommandResult.NotFound);
        }

        return new ScreenResult(Card(artist, mbid, searchOffset, searchText), CommandResult.Found);
    }

    private Screen Card(MusicArtist artist, string mbid, int searchOffset, string searchText)
    {
        var html = $"{ScreenText.Flag(artist.Country)} <b>{ScreenText.Html(artist.Name)}</b>";
        var description = ScreenText.Describe(artist);
        if (description.Length > 0)
        {
            html += $"\n{ScreenText.Html(description)}";
        }

        var biography = ScreenText.Biography(artist.Biography);
        if (biography.Length > 0)
        {
            html += $"\n<blockquote expandable>{ScreenText.Html(ScreenText.Shorten(biography, MaxBiographyLength))}</blockquote>";
        }

        // The buttons of the old commands still send new messages; they become screens in the next steps of #48.
        var keyboard = InlineKeyboardHelper.GetArtistInlineKeyboardMenu(mbid, _configuration.IsLastFmAvailable()).InlineKeyboard
            .Append(LastRow(searchOffset, searchText));
        return new Screen(html, new InlineKeyboardMarkup(keyboard), artist[MusicServiceType.LastFm]?.ExternalUrl?.ToString());
    }

    private InlineKeyboardButton[] LastRow(int searchOffset, string searchText)
    {
        return searchText.Length == 0
            ? new[] { CommonScreens.DeleteButton() }
            : new[] { InlineKeyboardButton.WithCallbackData("⬅️ Results", _searchScreens.SearchData(searchText, searchOffset)), CommonScreens.DeleteButton() };
    }
}
