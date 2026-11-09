using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

/// <summary>
/// Prototype of the 2.0.0 screens (#46) on real data, not for merge: one message per flow edited in place,
/// rich messages (tables, expandable quotes, footers), button styles, disabled buttons, copy buttons, short callback ids.
/// </summary>
public sealed partial class PrototypeHandlers : IBotHandlers
{
    private const int SetlistsPerScreen = 10;
    private const int SetlistsPerApiPage = 20;
    private const int ListenColumns = 6;

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private readonly IMusicSearcherClient _music;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<PrototypeHandlers> _logger;

    // Short callback ids (#48): the button keeps "p12", the payload stays here. Ids are lost on restart: "outdated button".
    private readonly ConcurrentDictionary<string, string> _payloads = new();
    private readonly ConcurrentDictionary<string, string> _ids = new();
    private int _nextId;

    public PrototypeHandlers(IMusicSearcherClient music, ITelegramBotClient bot, ILogger<PrototypeHandlers> logger)
    {
        _music = music;
        _bot = bot;
        _logger = logger;
    }

    private sealed record Screen(string Html, bool IsRich, InlineKeyboardMarkup Keyboard);

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is { Text: { } text } message)
            {
                await OnTextAsync(message.Chat.Id, text.Trim());
            }
            else if (update.CallbackQuery is { } query)
            {
                await OnCallbackAsync(query);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Prototype failed on update {UpdateId}", update.Id);
        }
    }

    private async Task OnTextAsync(long chatId, string text)
    {
        if (text.StartsWith('/'))
        {
            await _bot.SendMessage(chatId, "🎸 <b>Prototype of the new screens</b>\nSend an artist or band name.", ParseMode.Html);
            return;
        }

        var message = await _bot.SendMessage(chatId, "🔍 Searching…");
        await ShowAsync(chatId, message.MessageId, await SearchScreenAsync(text));
    }

    private async Task OnCallbackAsync(CallbackQuery query)
    {
        var chatId = query.Message!.Chat.Id;
        var messageId = query.Message.MessageId;
        if (query.Data == null || !_payloads.TryGetValue(query.Data, out var payload))
        {
            await _bot.AnswerCallbackQuery(query.Id, "This button is outdated. Send the artist name again.", showAlert: true);
            return;
        }

        var parts = payload.Split('|');
        string? answer = null;
        switch (parts[0])
        {
            case "artist":
                await ShowAsync(chatId, messageId, await ArtistScreenAsync(parts[1]));
                break;
            case "setlists":
                await ShowAsync(chatId, messageId, await SetlistsScreenAsync(parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture)));
                break;
            case "setlist":
                await ShowAsync(chatId, messageId, await SetlistScreenAsync(parts[1], parts[2], int.Parse(parts[3], CultureInfo.InvariantCulture), listen: false));
                break;
            case "listen":
                await ShowAsync(chatId, messageId, await SetlistScreenAsync(parts[1], parts[2], int.Parse(parts[3], CultureInfo.InvariantCulture), listen: true));
                break;
            case "top":
                await ShowAsync(chatId, messageId, await TopTracksScreenAsync(parts[1]));
                break;
            case "play":
                answer = await SendTrackAsync(chatId, parts[1], parts[2]);
                break;
            case "lyrics":
                answer = await SendLyricsAsync(chatId, parts[1], parts[2]);
                break;
            case "close":
                await _bot.DeleteMessage(chatId, messageId);
                break;
            case "hint":
                await ShowAsync(chatId, messageId, new Screen("🔍 Send an artist or band name.", false, new InlineKeyboardMarkup()));
                break;
        }

        // Every callback query is answered, otherwise the button keeps spinning.
        await _bot.AnswerCallbackQuery(query.Id, answer);
    }

    private async Task<Screen> SearchScreenAsync(string text)
    {
        var artists = (await _music.SearchArtistsByName(text)).ToList();
        if (artists.Count == 0)
        {
            return new Screen($"🤷 Nothing found for <b>{Encode(text)}</b>.\nCheck the spelling or try another name.", false, new InlineKeyboardMarkup());
        }

        var rows = artists.Select(a => new[] { Button($"{Flag(a.Country)} {a.Name} · {Describe(a)}", $"artist|{a.MBID}") });
        return new Screen($"Choose the artist for <b>{Encode(text)}</b>:", false, new InlineKeyboardMarkup(rows));
    }

    private async Task<Screen> ArtistScreenAsync(string mbid)
    {
        var artist = (MusicArtist)await _music.SearchArtistByMBID(mbid);
        var lastFm = artist[MusicServiceType.LastFm];
        var html = new StringBuilder();
        if (artist.ImageUri != null)
        {
            html.Append($"<img src=\"{artist.ImageUri}\">");
        }
        html.Append($"<h2>{Flag(artist.Country)} {Encode(artist.Name)}</h2>");
        html.Append($"<p>{Encode(Describe(artist))}</p>");

        var biography = CleanBiography(artist.Biography);
        if (biography.Length > 0)
        {
            html.Append($"<blockquote expandable>{Encode(biography)}</blockquote>");
        }
        if (lastFm?.ExternalUrl != null)
        {
            html.Append($"<footer>Biography from <a href=\"{lastFm.ExternalUrl}\">Last.fm</a></footer>");
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { Styled(Button("📝 Setlists", $"setlists|{mbid}|1"), KeyboardButtonStyle.Primary), Button("🎧 Top tracks", $"top|{mbid}") },
            new[] { Button("🔍 Another artist", "hint") },
        });
        return new Screen(html.ToString(), true, keyboard);
    }

    private async Task<Screen> SetlistsScreenAsync(string mbid, int screen)
    {
        var apiPage = ((screen - 1) * SetlistsPerScreen / SetlistsPerApiPage) + 1;
        var offset = (screen - 1) * SetlistsPerScreen % SetlistsPerApiPage;
        var setlists = await _music.SearchArtistSetlists(mbid, apiPage);
        if (setlists?.Setlist == null || setlists.Setlist.Count == 0)
        {
            return new Screen("No setlists on setlist.fm yet.", false, new InlineKeyboardMarkup(new[] { new[] { Button("⬅️ Artist", $"artist|{mbid}") } }));
        }

        var screens = (int)Math.Ceiling(setlists.Total / (double)SetlistsPerScreen);
        var rows = setlists.Setlist.Skip(offset).Take(SetlistsPerScreen)
            .Select(s => new[] { Button($"{(s.IsSetsExist() ? "" : "🔜 ")}{FormatDate(s.EventDate)} · {s.CityName} · {s.VenueName}", $"setlist|{s.Id}|{mbid}|{screen}") })
            .ToList();

        // Disabled arrows keep the layout steady on the first and the last page.
        rows.Add(new[]
        {
            screen > 1 ? Button("⬅️", $"setlists|{mbid}|{screen - 1}") : InlineKeyboardButton.WithDisabled("⬅️"),
            InlineKeyboardButton.WithDisabled($"{screen} / {screens}"),
            screen < screens ? Button("➡️", $"setlists|{mbid}|{screen + 1}") : InlineKeyboardButton.WithDisabled("➡️"),
        });
        rows.Add(new[] { Button("⬅️ Artist", $"artist|{mbid}") });

        var artistName = setlists.Setlist[0].ArtistName;
        return new Screen($"📝 <b>{Encode(artistName)}</b> · {setlists.Total} setlists\n🔜 marks an upcoming concert.", false, new InlineKeyboardMarkup(rows));
    }

    private async Task<Screen> SetlistScreenAsync(string setlistId, string mbid, int screen, bool listen)
    {
        var setlist = await _music.SearchSetlist(setlistId);
        var songs = setlist.Sets?.Set?.SelectMany(set => set.Songs ?? new List<MusicSong>()).ToList() ?? new List<MusicSong>();

        var html = new StringBuilder();
        html.Append($"<h3>{Encode(setlist.ArtistName)}</h3>");
        if (!string.IsNullOrEmpty(setlist.TourName))
        {
            html.Append($"<p><b>{Encode(setlist.TourName)}</b></p>");
        }
        html.Append($"<p>📍 {Encode(setlist.VenueName)}, {Encode(setlist.CityName)} ({Encode(setlist.CountryName)}) · {FormatDate(setlist.EventDate)}</p>");

        if (songs.Count == 0)
        {
            html.Append("<p>🔜 Upcoming concert: no setlist yet.</p>");
        }
        else
        {
            html.Append("<table><tr><th>#</th><th>Song</th><th>Note</th></tr>");
            var number = 1;
            foreach (var set in setlist.Sets!.Set)
            {
                var title = set.Encore > 0 ? (set.Encore > 1 ? $"Encore {set.Encore}" : "Encore") : set.Name;
                if (!string.IsNullOrEmpty(title))
                {
                    html.Append($"<tr><td colspan=\"3\"><b>{Encode(title)}</b></td></tr>");
                }
                foreach (var song in set.Songs ?? new List<MusicSong>())
                {
                    html.Append($"<tr><td>{number++}</td><td>{Encode(song.Name)}</td><td>{Encode(Note(song))}</td></tr>");
                }
            }
            html.Append("</table>");
        }
        html.Append($"<footer>Setlist from <a href=\"https://www.setlist.fm/search?query={Uri.EscapeDataString(setlist.ArtistName ?? "")}\">setlist.fm</a></footer>");

        var rows = new List<InlineKeyboardButton[]>();
        if (listen)
        {
            // Compact number grid instead of a button per song.
            rows.AddRange(songs.Select((song, i) => Button($"{i + 1}", $"play|{mbid}|{song.Name}"))
                .Chunk(ListenColumns));
            rows.Add(new[] { Button("✖️ Close player", $"setlist|{setlistId}|{mbid}|{screen}") });
        }
        else if (songs.Count > 0)
        {
            rows.Add(new[]
            {
                Styled(Button("🎧 Listen…", $"listen|{setlistId}|{mbid}|{screen}"), KeyboardButtonStyle.Primary),
                new InlineKeyboardButton("📋 Copy") { CopyText = new CopyTextButton { Text = CopyTextOf(setlist, songs) } },
            });
        }
        rows.Add(new[] { Button("⬅️ Setlists", $"setlists|{mbid}|{screen}") });
        return new Screen(html.ToString(), true, new InlineKeyboardMarkup(rows));
    }

    private async Task<Screen> TopTracksScreenAsync(string mbid)
    {
        var tracks = (await _music.SearchTopTracks(mbid)).ToList();
        var rows = tracks.Select((t, i) => new[] { Button($"{i + 1}. {t.TrackName}", $"play|{mbid}|{t.TrackName}") }).ToList();
        rows.Add(new[] { Button("⬅️ Artist", $"artist|{mbid}") });
        return new Screen("🎧 <b>Top tracks</b> on Last.fm", false, new InlineKeyboardMarkup(rows));
    }

    private async Task<string?> SendTrackAsync(long chatId, string mbid, string song)
    {
        var artist = await _music.SearchArtistByMBID(mbid);
        var track = await _music.SearchTrack(artist.Name!, song);
        if (string.IsNullOrEmpty(track.DownloadLink))
        {
            return $"No audio for {song}";
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                Button("🎼 Lyrics", $"lyrics|{mbid}|{song}"),
                new InlineKeyboardButton("📋 Copy name") { CopyText = new CopyTextButton { Text = $"{artist.Name} – {song}" } },
                Styled(Button("🗑", "close"), KeyboardButtonStyle.Danger),
            },
        });
        await _bot.SendAudio(chatId, InputFile.FromString(track.DownloadLink), performer: artist.Name, title: song, replyMarkup: keyboard);
        return null;
    }

    private async Task<string?> SendLyricsAsync(long chatId, string mbid, string song)
    {
        var artist = await _music.SearchArtistByMBID(mbid);
        var lyric = await _music.SearchLyric(artist.Name!, song);
        if (!lyric.IsSuccessSearchResult || string.IsNullOrWhiteSpace(lyric.LyricText))
        {
            return lyric.Instrumental ? "Instrumental, no lyrics" : $"No lyrics found for {song}";
        }

        var lines = string.Join("<br/>", lyric.LyricText.Trim().Split('\n').Select(line => Encode(line.TrimEnd('\r'))));
        var html = $"<h4>🎼 {Encode(song)} · {Encode(artist.Name)}</h4><blockquote expandable>{lines}</blockquote>";
        var keyboard = new InlineKeyboardMarkup(new[] { new[] { Styled(Button("🗑 Close", "close"), KeyboardButtonStyle.Danger) } });
        await _bot.SendRichMessage(chatId, new InputRichMessage { Html = html }, replyMarkup: keyboard);
        return null;
    }

    private async Task ShowAsync(long chatId, int messageId, Screen screen)
    {
        try
        {
            await EditAsync(chatId, messageId, screen);
        }
        catch (ApiRequestException exception) when (exception.Message.Contains("not modified"))
        {
        }
        catch (ApiRequestException exception) when (screen.IsRich && screen.Html.Contains("<img"))
        {
            // Prototype check: does Telegram accept an image in a rich message edit?
            _logger.LogWarning("Rich edit with an image failed: {Reason}. Retrying without the image", exception.Message);
            await EditAsync(chatId, messageId, screen with { Html = ImageTag().Replace(screen.Html, "") });
        }
    }

    private Task EditAsync(long chatId, int messageId, Screen screen)
    {
        return screen.IsRich
            ? _bot.EditMessageText(chatId, messageId, null, replyMarkup: screen.Keyboard, richMessage: new InputRichMessage { Html = screen.Html })
            : _bot.EditMessageText(chatId, messageId, screen.Html, ParseMode.Html, screen.Keyboard, new LinkPreviewOptions { IsDisabled = true });
    }

    private InlineKeyboardButton Button(string text, string payload)
    {
        var id = _ids.GetOrAdd(payload, _ => $"p{Interlocked.Increment(ref _nextId)}");
        _payloads[id] = payload;
        return InlineKeyboardButton.WithCallbackData(text, id);
    }

    private static InlineKeyboardButton Styled(InlineKeyboardButton button, KeyboardButtonStyle style)
    {
        button.Style = style;
        return button;
    }

    private static string Describe(MusicArtistBase artist)
    {
        var years = artist.ActiveYears?.Trim(' ', '-');
        var parts = new[] { artist.Type, artist.Area, string.IsNullOrEmpty(years) ? null : artist.ActiveYears!.Trim() };
        return string.Join(" · ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string Flag(string? country)
    {
        if (country is not { Length: 2 } || !country.All(char.IsAsciiLetter))
        {
            return "🎤";
        }
        return string.Concat(country.ToUpperInvariant().Select(c => char.ConvertFromUtf32(0x1F1E6 + c - 'A')));
    }

    private static string Note(MusicSong song)
    {
        var notes = new[] { song.CoverName == null ? null : $"{song.CoverName} cover", song.WithName == null ? null : $"with {song.WithName}", song.Info };
        return string.Join("; ", notes.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string CopyTextOf(MusicSetlist setlist, List<MusicSong> songs)
    {
        var text = $"{setlist.ArtistName} · {FormatDate(setlist.EventDate)} · {setlist.CityName}\n"
            + string.Join("\n", songs.Select((s, i) => $"{i + 1}. {s.Name}"));
        return text.Length <= 256 ? text : text[..255] + "…";
    }

    private static string FormatDate(string? date)
    {
        return DateTime.TryParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.ToString("d MMM yyyy", English)
            : date ?? "";
    }

    private static string CleanBiography(string? biography)
    {
        if (string.IsNullOrWhiteSpace(biography))
        {
            return "";
        }
        var text = HtmlTag().Replace(biography, "");
        return WebUtility.HtmlDecode(text).Replace("Read more on Last.fm", "").Trim();
    }

    private static string Encode(string? text) => WebUtility.HtmlEncode(text ?? "");

    [GeneratedRegex("<.*?>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex("<img [^>]*>")]
    private static partial Regex ImageTag();
}
