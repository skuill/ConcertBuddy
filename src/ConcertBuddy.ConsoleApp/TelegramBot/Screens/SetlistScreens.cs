using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using MusicSearcher;
using MusicSearcher.Model;
using System.Globalization;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// The setlists of an artist page by page, and one setlist as a rich message with a table of songs.
/// </summary>
public sealed class SetlistScreens
{
    /// <summary>
    /// Ten fit a phone screen; setlist.fm gives twenty per request, so two screens share one request.
    /// </summary>
    public const int SetlistsPerScreen = 10;

    public const int SetlistsPerApiPage = 20;

    /// <summary>
    /// The limit of a copy_text button. Telegram counts it in UTF-8 bytes: 256 characters with "·" were rejected
    /// as BUTTON_DATA_INVALID on the dev bot.
    /// </summary>
    public const int MaxCopyTextBytes = 256;

    private readonly IMusicSearcherClient _musicSearcher;
    private readonly CallbackCodec _codec;

    public SetlistScreens(IMusicSearcherClient musicSearcher, CallbackCodec codec)
    {
        _musicSearcher = musicSearcher;
        _codec = codec;
    }

    public async Task<ScreenResult> SetlistsAsync(string mbid, int screen)
    {
        var apiPage = ((screen - 1) * SetlistsPerScreen / SetlistsPerApiPage) + 1;
        var skip = (screen - 1) * SetlistsPerScreen % SetlistsPerApiPage;
        MusicSetlists? setlists;
        try
        {
            setlists = await _musicSearcher.SearchArtistSetlists(mbid, apiPage);
        }
        catch (MusicServiceUnavailableException exception)
        {
            return CommonScreens.Unavailable(exception.Service, SetlistsData(mbid, screen));
        }

        var page = setlists?.Setlist?.Skip(skip).Take(SetlistsPerScreen).ToList() ?? new List<MusicSetlist>();
        if (page.Count == 0)
        {
            var html = "📭 No setlists of this artist on setlist.fm yet.";
            return new ScreenResult(new Screen(html, new InlineKeyboardMarkup(new[] { new[] { ArtistButton(mbid) } })), CommandResult.NotFound);
        }

        var screens = Math.Max(1, (int)Math.Ceiling(setlists!.Total / (double)SetlistsPerScreen));
        var rows = page
            .Select(setlist => new[] { InlineKeyboardButton.WithCallbackData(Label(setlist), SetlistData(setlist.Id ?? string.Empty, mbid, screen)) })
            .ToList();

        // Disabled arrows keep the row steady on the first and the last screen.
        rows.Add(new[]
        {
            screen > 1 ? InlineKeyboardButton.WithCallbackData("⬅️", SetlistsData(mbid, screen - 1)) : InlineKeyboardButton.WithDisabled("⬅️"),
            InlineKeyboardButton.WithDisabled($"{screen} / {screens}"),
            screen < screens ? InlineKeyboardButton.WithCallbackData("➡️", SetlistsData(mbid, screen + 1)) : InlineKeyboardButton.WithDisabled("➡️"),
        });
        rows.Add(new[] { ArtistButton(mbid) });

        var header = $"📝 <b>{ScreenText.Html(page[0].ArtistName)}</b> · {setlists.Total} setlists\n🔜 upcoming · 📭 no songs on setlist.fm yet";
        return new ScreenResult(new Screen(header, new InlineKeyboardMarkup(rows)), CommandResult.Found);
    }

    public async Task<ScreenResult> SetlistAsync(string setlistId, string mbid, int screen)
    {
        MusicSetlist? setlist;
        try
        {
            setlist = await _musicSearcher.SearchSetlist(setlistId);
        }
        catch (MusicServiceUnavailableException exception)
        {
            return CommonScreens.Unavailable(exception.Service, SetlistData(setlistId, mbid, screen));
        }

        var back = new[] { InlineKeyboardButton.WithCallbackData("⬅️ Setlists", SetlistsData(mbid, screen)) };
        if (setlist == null)
        {
            var html = "🤷 setlist.fm does not have this setlist any more.";
            return new ScreenResult(new Screen(html, new InlineKeyboardMarkup(new[] { back })), CommandResult.NotFound);
        }

        var sets = setlist.Sets?.Set ?? new List<MusicSet>();
        var songs = sets.SelectMany(set => set.Songs ?? new List<MusicSong>()).ToList();
        var rows = new List<InlineKeyboardButton[]>();
        if (songs.Count > 0)
        {
            rows.Add(new[] { new InlineKeyboardButton("📋 Copy setlist") { CopyText = new CopyTextButton { Text = CopyText(setlist, songs) } } });
        }
        rows.Add(back);
        return new ScreenResult(new Screen(SetlistHtml(setlist, sets, songs, mbid), new InlineKeyboardMarkup(rows), IsRich: true), CommandResult.Found);
    }

    public string SetlistsData(string mbid, int screen)
    {
        return _codec.Encode(CallbackActions.Setlists, mbid, Number(screen));
    }

    private string SetlistData(string setlistId, string mbid, int screen)
    {
        return _codec.Encode(CallbackActions.Setlist, setlistId, mbid, Number(screen));
    }

    private InlineKeyboardButton ArtistButton(string mbid)
    {
        return InlineKeyboardButton.WithCallbackData("⬅️ Artist", _codec.Encode(CallbackActions.Artist, mbid, Number(0), string.Empty));
    }

    private static string SetlistHtml(MusicSetlist setlist, List<MusicSet> sets, List<MusicSong> songs, string mbid)
    {
        var html = new StringBuilder();
        html.Append($"<h3>{ScreenText.Html(setlist.ArtistName)}</h3>");
        if (!string.IsNullOrEmpty(setlist.TourName))
        {
            html.Append($"<p><b>{ScreenText.Html(setlist.TourName)}</b></p>");
        }
        html.Append($"<p>📍 {ScreenText.Html(Place(setlist))} · {ScreenText.Date(setlist.EventDate)}</p>");

        if (songs.Count == 0)
        {
            html.Append(IsFuture(setlist) ? "<p>🔜 Upcoming concert: no setlist yet.</p>" : "<p>📭 Nobody has added the songs on setlist.fm yet.</p>");
        }
        else
        {
            html.Append($"<p><i>{ScreenText.Html(Summary(sets, songs))}</i></p>");
            AppendTable(html, sets, songs, mbid);
        }

        if (!string.IsNullOrWhiteSpace(setlist.Info))
        {
            html.Append($"<blockquote>ℹ️ {ScreenText.Html(setlist.Info)}</blockquote>");
        }

        // setlist.fm asks to link its page wherever its data is shown.
        var url = setlist.Url ?? $"https://www.setlist.fm/search?query={Uri.EscapeDataString(setlist.ArtistName ?? string.Empty)}";
        html.Append($"<footer>{(songs.Count > 0 ? "Tap a song to listen · " : string.Empty)}Setlist from <a href=\"{ScreenText.Html(url)}\">setlist.fm</a></footer>");
        return html.ToString();
    }

    private static void AppendTable(StringBuilder html, List<MusicSet> sets, List<MusicSong> songs, string mbid)
    {
        var hasNotes = songs.Any(song => Note(song).Length > 0);
        var columns = hasNotes ? 3 : 2;
        html.Append($"<table bordered striped><tr><th>#</th><th>Song</th>{(hasNotes ? "<th>Note</th>" : string.Empty)}</tr>");
        var number = 1;
        foreach (var set in sets)
        {
            var title = set.Encore > 0 ? (set.Encore > 1 ? $"Encore {set.Encore}" : "Encore") : set.Name;
            if (!string.IsNullOrEmpty(title))
            {
                html.Append($"<tr><td colspan=\"{columns}\"><b>🔁 {ScreenText.Html(title)}</b></td></tr>");
            }

            foreach (var song in set.Songs ?? new List<MusicSong>())
            {
                // The song name is a callback button styled as a link; a tape is not a song to listen to.
                var name = song.Tape || string.IsNullOrWhiteSpace(song.Name)
                    ? $"<i>{ScreenText.Html(song.Name)}</i>"
                    : $"<tg-button type=\"callback_data\" style=\"link\" data=\"{ScreenText.Html(InlineKeyboardHelper.GetTrackCallbackData(mbid, song.Name))}\">{ScreenText.Html(song.Name)}</tg-button>";
                var note = hasNotes ? $"<td>{ScreenText.Html(Note(song))}</td>" : string.Empty;
                html.Append($"<tr><td>{(song.Tape ? string.Empty : Number(number++))}</td><td>{name}</td>{note}</tr>");
            }
        }
        html.Append("</table>");
    }

    private static string Label(MusicSetlist setlist)
    {
        var mark = IsFuture(setlist) ? "🔜 " : setlist.IsSetsExist() ? string.Empty : "📭 ";
        var parts = new[] { ScreenText.Date(setlist.EventDate), setlist.CityName, setlist.VenueName };
        return mark + string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string Place(MusicSetlist setlist)
    {
        var city = string.Join(", ", new[] { setlist.CityName, setlist.CountryName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(setlist.VenueName) ? city : $"{setlist.VenueName}, {city}";
    }

    private static string Summary(List<MusicSet> sets, List<MusicSong> songs)
    {
        var parts = new List<string> { Count(songs.Count(song => !song.Tape), "song", "songs") };
        var covers = songs.Count(song => song.CoverName != null);
        var guests = songs.Count(song => song.WithName != null);
        var encores = sets.Count(set => set.Encore > 0);
        if (covers > 0)
        {
            parts.Add(Count(covers, "cover", "covers"));
        }
        if (guests > 0)
        {
            parts.Add($"{guests} with guests");
        }
        if (encores > 0)
        {
            parts.Add(encores == 1 ? "encore" : $"{encores} encores");
        }
        return string.Join(" · ", parts);
    }

    private static string Note(MusicSong song)
    {
        var notes = new[]
        {
            song.Tape ? "📼 from tape" : null,
            song.CoverName == null ? null : $"🎸 {song.CoverName} cover",
            song.WithName == null ? null : $"👥 with {song.WithName}",
            string.IsNullOrWhiteSpace(song.Info) ? null : $"ℹ️ {song.Info}",
        };
        return string.Join("; ", notes.Where(note => note != null));
    }

    private static string CopyText(MusicSetlist setlist, List<MusicSong> songs)
    {
        // Whole lines only: a long setlist ends with "…" after the last song that fits.
        const string more = "\n…";
        var lines = songs.Where(song => !song.Tape).Select((song, index) => $"{index + 1}. {song.Name}")
            .Prepend($"{setlist.ArtistName} · {ScreenText.Date(setlist.EventDate)} · {setlist.CityName}")
            .ToList();
        var text = string.Join("\n", lines);
        while (Encoding.UTF8.GetByteCount(text) > MaxCopyTextBytes && lines.Count > 1)
        {
            lines.RemoveAt(lines.Count - 1);
            text = string.Join("\n", lines) + more;
        }
        return StringHelper.SubstringByByteLength(text, MaxCopyTextBytes);
    }

    private static bool IsFuture(MusicSetlist setlist)
    {
        return DateTime.TryParseExact(setlist.EventDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) && setlist.IsFutureEvent();
    }

    private static string Count(int count, string one, string many)
    {
        return $"{count} {(count == 1 ? one : many)}";
    }

    private static string Number(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
