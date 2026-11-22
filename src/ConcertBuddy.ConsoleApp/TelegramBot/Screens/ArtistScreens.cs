using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Microsoft.Extensions.Options;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.MusicService;
using System.Globalization;
using System.Text;
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
    private readonly Configuration _configuration;

    public ArtistScreens(IMusicSearcherClient musicSearcher, CallbackCodec codec, IOptions<Configuration> configuration)
    {
        _musicSearcher = musicSearcher;
        _codec = codec;
        _configuration = configuration.Value;
    }

    /// <summary>
    /// The card of the artist; the search text and offset lead back to the results (an empty text: no search, no button).
    /// A card that a confident match opened at once (#94) calls that button "Not this artist?".
    /// </summary>
    public async Task<ScreenResult> CardAsync(string mbid, int searchOffset, string searchText, bool confidentMatch = false)
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

        return new ScreenResult(Card(artist, mbid, searchOffset, searchText, confidentMatch), CommandResult.Found);
    }

    private Screen Card(MusicArtist artist, string mbid, int searchOffset, string searchText, bool confidentMatch)
    {
        var html = $"{ScreenText.Flag(artist.Country)} <b>{ScreenText.Html(artist.Name)}</b>";
        var description = ScreenText.Describe(artist);
        if (description.Length > 0)
        {
            html += $"\n{ScreenText.Html(description)}";
        }

        // MusicBrainz did not answer and the card came from Last.fm (#91).
        if (!artist.IsMusicArtistExist(MusicServiceType.MusicBrainz))
        {
            html += $"\n<i>{SearchScreens.LastFmNote}</i>";
        }

        var biography = ScreenText.Biography(artist.Biography);
        if (biography.Length > 0)
        {
            html += $"\n<blockquote expandable>{ScreenText.Html(ScreenText.Shorten(biography, MaxBiographyLength))}</blockquote>";
        }

        var setlists = InlineKeyboardButton.WithCallbackData("📝 Setlists", _codec.Encode(CallbackActions.Setlists, mbid, "1"));
        setlists.Style = KeyboardButtonStyle.Primary;
        var fullBiography = InlineKeyboardButton.WithCallbackData("📖 Full biography", _codec.Encode(CallbackActions.Biography, mbid));
        InlineKeyboardButton[] more = _configuration.IsLastFmAvailable()
            ? new[] { fullBiography, InlineKeyboardButton.WithCallbackData("📻 Top tracks", _codec.Encode(CallbackActions.TopTracks, mbid)) }
            : new[] { fullBiography };
        var rows = new List<InlineKeyboardButton[]>
        {
            new[] { setlists, InlineKeyboardButton.WithCallbackData("📅 Upcoming", _codec.Encode(CallbackActions.Upcoming, mbid)) },
            more,
            LastRow(searchOffset, searchText, confidentMatch),
        };
        return new Screen(html, new InlineKeyboardMarkup(rows), artist[MusicServiceType.LastFm]?.ExternalUrl?.ToString());
    }

    /// <summary>
    /// The whole Last.fm biography as a rich message, credited to Last.fm as its license asks.
    /// </summary>
    public async Task<ScreenResult> BiographyAsync(string mbid)
    {
        MusicArtist artist;
        try
        {
            artist = (MusicArtist)await _musicSearcher.SearchArtistByMBID(mbid);
        }
        catch (MusicServiceUnavailableException exception)
        {
            return CommonScreens.Unavailable(exception.Service, _codec.Encode(CallbackActions.Biography, mbid));
        }

        var back = new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Artist", _codec.Encode(CallbackActions.Artist, mbid, "0", string.Empty)) } };
        var biography = ScreenText.Biography(artist.FullBiography ?? artist.Biography);
        if (biography.Length == 0)
        {
            var html = $"📖 Last.fm has no biography of <b>{ScreenText.Html(artist.Name)}</b> yet.";
            return new ScreenResult(new Screen(html, new InlineKeyboardMarkup(back)), CommandResult.NotFound);
        }

        var rich = new StringBuilder($"<h3>📖 {ScreenText.Html(artist.Name)}</h3>");
        foreach (var paragraph in biography.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            rich.Append($"<p>{ScreenText.Html(paragraph)}</p>");
        }
        var lastFmUrl = artist[MusicServiceType.LastFm]?.ExternalUrl;
        if (lastFmUrl != null)
        {
            rich.Append($"<footer>Biography from <a href=\"{ScreenText.Html(lastFmUrl.ToString())}\">Last.fm</a>, user-contributed text under CC BY-SA</footer>");
        }
        return new ScreenResult(new Screen(rich.ToString(), new InlineKeyboardMarkup(back), IsRich: true), CommandResult.Found);
    }

    private InlineKeyboardButton[] LastRow(int searchOffset, string searchText, bool confidentMatch = false)
    {
        return searchText.Length == 0
            ? new[] { CommonScreens.DeleteButton() }
            : new[] { InlineKeyboardButton.WithCallbackData(confidentMatch ? "🔄 Not this artist?" : "⬅️ Results", SearchScreens.SearchData(_codec, searchText, searchOffset)), CommonScreens.DeleteButton() };
    }
}
