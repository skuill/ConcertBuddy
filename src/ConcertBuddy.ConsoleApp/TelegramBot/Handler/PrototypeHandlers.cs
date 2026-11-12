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
using Telegram.Bot.Types.InlineQueryResults;
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

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private readonly IMusicSearcherClient _music;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<PrototypeHandlers> _logger;

    // Short callback ids (#48): the button keeps "p12", the payload stays here. Ids are lost on restart: "outdated button".
    private readonly ConcurrentDictionary<string, string> _payloads = new();
    private readonly ConcurrentDictionary<string, string> _ids = new();
    private int _nextId;
    private string? _botUsername;

    public PrototypeHandlers(IMusicSearcherClient music, ITelegramBotClient bot, ILogger<PrototypeHandlers> logger)
    {
        _music = music;
        _bot = bot;
        _logger = logger;
    }

    private static readonly HttpClient AudioClient = new();

    private sealed record Screen(string Html, bool IsRich, InlineKeyboardMarkup Keyboard, string? PreviewUrl = null);

    public Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        // Prototype: every update runs on its own, so a slow track download does not hold the other buttons.
        _ = Task.Run(() => ProcessAsync(update), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task ProcessAsync(Update update)
    {
        try
        {
            if (update.Message is { Text: not null } message)
            {
                await OnMessageAsync(message);
            }
            else if (update.CallbackQuery is { } query)
            {
                await OnCallbackAsync(query);
            }
            else if (update.InlineQuery is { } inlineQuery)
            {
                await OnInlineQueryAsync(inlineQuery);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Prototype failed on update {UpdateId}", update.Id);
        }
    }

    private static readonly string[] Examples = { "Metallica", "Parkway Drive", "IDLES" };

    private async Task OnMessageAsync(Message message)
    {
        await EnsureCommandMenuAsync();
        var chatId = message.Chat.Id;
        var text = message.Text!.Trim();
        var isGroup = message.Chat.Type is ChatType.Group or ChatType.Supergroup;

        // Commands may come as "/search@bot name" in groups.
        var command = text.StartsWith('/') ? text.Split(' ', 2)[0].Split('@')[0].ToLowerInvariant() : null;
        var argument = text.Contains(' ') ? text.Split(' ', 2)[1].Trim() : "";

        // Deep link from a shared setlist: t.me/<bot>?start=s_<setlist id>
        if (command == "/start" && argument.StartsWith("s_", StringComparison.Ordinal))
        {
            var setlistId = argument["s_".Length..];
            var loading = await _bot.SendMessage(chatId, "📝 Opening the shared setlist…");
            var shared = await _music.SearchSetlist(setlistId);
            await ShowAsync(chatId, loading.MessageId, await SetlistScreenAsync(setlistId, shared.ArtistMBID!, 1, asTable: true));
            return;
        }

        switch (command)
        {
            case "/start":
                await SendScreenAsync(chatId, StartScreen(message.From?.FirstName));
                return;
            case "/help":
                await SendScreenAsync(chatId, HelpScreen(isGroup));
                return;
            case "/about":
                await SendScreenAsync(chatId, AboutScreen());
                return;
            case "/search" when argument.Length == 0:
                await _bot.SendMessage(chatId, "Add a name: <code>/search Metallica</code>", ParseMode.Html);
                return;
            case "/search":
                text = argument;
                break;
            case not null:
                return;
        }

        if (isGroup)
        {
            await GroupSearchAsync(message, text);
            return;
        }

        var searching = await _bot.SendMessage(chatId, "🔍 Searching…");
        await ShowAsync(chatId, searching.MessageId, await SearchScreenAsync(text));
    }

    private bool _commandMenuSet;

    private async Task EnsureCommandMenuAsync()
    {
        if (_commandMenuSet)
        {
            return;
        }
        _commandMenuSet = true;
        var commands = new[]
        {
            new BotCommand { Command = "search", Description = "Find an artist: /search Metallica" },
            new BotCommand { Command = "help", Description = "How to use the bot" },
            new BotCommand { Command = "about", Description = "Sources, code and support" },
        };
        await _bot.SetMyCommands(commands);
        await _bot.SetMyCommands(commands, new BotCommandScopeAllGroupChats());
    }

    private async Task SendScreenAsync(long chatId, Screen screen)
    {
        await _bot.SendMessage(chatId, screen.Html, ParseMode.Html, replyMarkup: screen.Keyboard, linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true });
    }

    private Screen StartScreen(string? firstName)
    {
        var html = $"👋 Hi{(firstName == null ? "" : $", {Encode(firstName)}")}!\n\n"
            + "🎸 <b>ConcertBuddy</b> finds what bands play live: setlists of past and upcoming concerts, tracks and lyrics.\n\n"
            + "Send an artist or band name, or try one of these:";
        var rows = new List<InlineKeyboardButton[]>
        {
            Examples.Select(name => Button(name, $"search|{name}")).ToArray(),
            new[] { Button("❓ Help", "help"), Button("ℹ️ About", "about") },
        };
        return new Screen(html, false, new InlineKeyboardMarkup(rows));
    }

    private Screen HelpScreen(bool isGroup)
    {
        var html = "❓ <b>How to use ConcertBuddy</b>\n\n"
            + "🔍 <b>Search:</b> send an artist name" + (isGroup ? " as <code>/search Metallica</code> (in groups the bot sees only commands)" : "") + ".\n"
            + "📝 <b>Setlists:</b> open the artist, then Setlists. Tap a song to listen.\n"
            + "📅 <b>Upcoming:</b> concerts that fans already added on setlist.fm.\n"
            + "🎼 <b>Lyrics:</b> under every track.\n"
            + "↗️ <b>Share:</b> \"Send to a chat\" under a setlist, or type <code>@test_concert_buddy_bot</code> in any chat.\n\n"
            + "In groups the list of artists is shown only to the one who asked; the result is for everyone.";
        return new Screen(html, false, new InlineKeyboardMarkup(new[] { new[] { Button("ℹ️ About", "about") } }));
    }

    private static Screen AboutScreen()
    {
        var version = typeof(PrototypeHandlers).Assembly.GetName().Version?.ToString(3);
        var html = $"ℹ️ <b>ConcertBuddy</b> {version}\n\n"
            + "Data: <a href=\"https://musicbrainz.org\">MusicBrainz</a>, <a href=\"https://www.setlist.fm\">setlist.fm</a>, "
            + "<a href=\"https://www.last.fm\">Last.fm</a>, Yandex Music, lyrics via <a href=\"https://github.com/skuill/LyricsScraperNET\">LyricsScraperNET</a>.\n"
            + "Open source: <a href=\"https://github.com/skuill/ConcertBuddy\">github.com/skuill/ConcertBuddy</a>\n\n"
            + "💛 The bot is free. If it helps you, you can support it:";
        var rows = new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("☕ Buy me a coffee", "https://www.buymeacoffee.com/skuill"),
                InlineKeyboardButton.WithUrl("💳 PayPal", "https://paypal.me/VitalyKvashin"),
            },
            new[] { InlineKeyboardButton.WithUrl("🪙 Crypto", "https://github.com/skuill/ConcertBuddy#support") },
        };
        return new Screen(html, false, new InlineKeyboardMarkup(rows));
    }

    private async Task GroupSearchAsync(Message message, string text)
    {
        var chatId = message.Chat.Id;
        Screen screen;
        try
        {
            screen = await SearchListScreenAsync(text, pickPayload: "gpick");
        }
        catch (Exception)
        {
            screen = UnavailableScreen(text);
        }

        try
        {
            // The choice is visible only to the one who asked (ephemeral message); the chosen artist is posted for everyone.
            await _bot.SendMessage(chatId, screen.Html, ParseMode.Html, replyMarkup: screen.Keyboard,
                ephemeralMessageParameters: new EphemeralMessageParameters { ReceiverUserId = message.From!.Id });
        }
        catch (ApiRequestException exception)
        {
            _logger.LogWarning("Ephemeral message rejected: {Reason}. Showing the list to everyone", exception.Message);
            await SendScreenAsync(chatId, screen);
        }
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
        if (parts[0] == "gpick" && query.Message.EphemeralMessageId is int ephemeralId)
        {
            await _bot.AnswerCallbackQuery(query.Id);
            await _bot.DeleteEphemeralMessage(chatId, query.From.Id, ephemeralId);
            var card = await ArtistScreenAsync(parts[1]);
            await _bot.SendMessage(chatId, $"{card.Html}\n\n<i>Asked by {Encode(query.From.FirstName)}</i>", ParseMode.Html, replyMarkup: card.Keyboard,
                linkPreviewOptions: card.PreviewUrl == null ? new LinkPreviewOptions { IsDisabled = true } : new LinkPreviewOptions { Url = card.PreviewUrl, ShowAboveText = true, PreferLargeMedia = true });
            return;
        }

        switch (parts[0])
        {
            case "play":
                // Slow actions answer at once, so the user sees that the tap worked.
                await _bot.AnswerCallbackQuery(query.Id, $"⏳ Loading {parts[2]}…");
                await SendTrackAsync(chatId, parts[1], parts[2]);
                return;
            case "lyrics":
                await _bot.AnswerCallbackQuery(query.Id, "⏳ Looking for the lyrics…");
                await SendLyricsAsync(chatId, messageId, parts[1], parts[2]);
                return;
            case "rlyrics":
                await _bot.AnswerCallbackQuery(query.Id, "⏳ Looking for the lyrics…");
                await ShowTrackLyricsAsync(chatId, messageId, parts[1], parts[2], show: true);
                return;
            case "rhide":
                await _bot.AnswerCallbackQuery(query.Id);
                await ShowTrackLyricsAsync(chatId, messageId, parts[1], parts[2], show: false);
                return;
            case "hidelyrics":
                await _bot.AnswerCallbackQuery(query.Id);
                await _bot.DeleteMessage(chatId, int.Parse(parts[1], CultureInfo.InvariantCulture));
                await _bot.EditMessageReplyMarkup(chatId, messageId, AudioKeyboard(parts[2], parts[3], parts[4], lyricsMessageId: null));
                return;
        }

        switch (parts[0])
        {
            case "artist":
            case "gpick":
                await ShowAsync(chatId, messageId, await ArtistScreenAsync(parts[1]));
                break;
            case "search":
                await ShowAsync(chatId, messageId, new Screen("🔍 Searching…", false, new InlineKeyboardMarkup()));
                await ShowAsync(chatId, messageId, await SearchScreenAsync(parts[1]));
                break;
            case "list":
                await ShowAsync(chatId, messageId, await SearchListScreenSafeAsync(parts[1]));
                break;
            case "upcoming":
                await ShowAsync(chatId, messageId, await UpcomingScreenAsync(parts[1]));
                break;
            case "help":
                await ShowAsync(chatId, messageId, HelpScreen(query.Message.Chat.Type is ChatType.Group or ChatType.Supergroup));
                break;
            case "about":
                await ShowAsync(chatId, messageId, AboutScreen());
                break;
            case "bio":
                await ShowAsync(chatId, messageId, await BiographyScreenAsync(parts[1]));
                break;
            case "setlists":
                await ShowAsync(chatId, messageId, await SetlistsScreenAsync(parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture)));
                break;
            case "setlist":
                var screen = int.Parse(parts[3], CultureInfo.InvariantCulture);
                try
                {
                    await ShowAsync(chatId, messageId, await SetlistScreenAsync(parts[1], parts[2], screen, asTable: true));
                }
                catch (ApiRequestException exception)
                {
                    // Prototype check: are buttons allowed in table cells? If not, the songs become a list.
                    _logger.LogWarning("Setlist table with buttons rejected: {Reason}. Showing a list", exception.Message);
                    await ShowAsync(chatId, messageId, await SetlistScreenAsync(parts[1], parts[2], screen, asTable: false));
                }
                break;
            case "top":
                await ShowAsync(chatId, messageId, await TopTracksScreenAsync(parts[1]));
                break;
            case "close":
                await _bot.DeleteMessage(chatId, messageId);
                break;
            case "hint":
                await ShowAsync(chatId, messageId, new Screen("🔍 Send an artist or band name.", false, new InlineKeyboardMarkup()));
                break;
        }

        // Every callback query is answered, otherwise the button keeps spinning.
        await _bot.AnswerCallbackQuery(query.Id);
    }

    private async Task<Screen> SearchScreenAsync(string text)
    {
        List<MusicArtistBase> artists;
        try
        {
            artists = (await _music.SearchArtistsByName(text)).ToList();
        }
        catch (Exception)
        {
            return UnavailableScreen(text);
        }

        if (artists.Count == 0)
        {
            return await NothingFoundScreenAsync(text);
        }

        // Confident match: the name is exactly what the user typed and no other result has the same name.
        var exact = artists.Where(a => string.Equals(a.Name?.Trim(), text.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count == 1)
        {
            return await ArtistScreenAsync(exact[0].MBID!, searchText: text);
        }
        return ListScreen(text, artists, "artist");
    }

    private async Task<Screen> SearchListScreenSafeAsync(string text)
    {
        try
        {
            return await SearchListScreenAsync(text, "artist");
        }
        catch (Exception)
        {
            return UnavailableScreen(text);
        }
    }

    private async Task<Screen> SearchListScreenAsync(string text, string pickPayload)
    {
        var artists = (await _music.SearchArtistsByName(text)).ToList();
        return artists.Count == 0 ? await NothingFoundScreenAsync(text) : ListScreen(text, artists, pickPayload);
    }

    private Screen ListScreen(string text, List<MusicArtistBase> artists, string pickPayload)
    {
        var rows = artists.Select(a => new[] { Button($"{Flag(a.Country)} {a.Name} · {Describe(a)}", $"{pickPayload}|{a.MBID}") });
        return new Screen($"Choose the artist for <b>{Encode(text)}</b>:", false, new InlineKeyboardMarkup(rows));
    }

    private async Task<Screen> NothingFoundScreenAsync(string text)
    {
        // A text typed with the wrong keyboard layout ("ьуефддшсф" for "metallica") is the most common miss after typos.
        var swapped = SwapLayout(text);
        if (swapped != text)
        {
            var found = (await _music.SearchArtistsByName(swapped)).ToList();
            if (found.Count > 0)
            {
                var rows = found.Select(a => new[] { Button($"{Flag(a.Country)} {a.Name} · {Describe(a)}", $"artist|{a.MBID}") });
                return new Screen($"🤔 Nothing for <b>{Encode(text)}</b>. Did you mean <b>{Encode(swapped)}</b>?", false, new InlineKeyboardMarkup(rows));
            }
        }

        return new Screen(
            $"🤷 Nothing found for <b>{Encode(text)}</b>.\n\n"
            + "• Check the spelling\n• Send the band name, not a song\n• Try the original spelling (Кино, not Kino)",
            false,
            new InlineKeyboardMarkup(new[] { Examples.Select(name => Button(name, $"search|{name}")).ToArray() }));
    }

    private Screen UnavailableScreen(string text)
    {
        return new Screen(
            "⚠️ MusicBrainz, the artist database, is busy right now. It is not your query: try again in a minute.",
            false,
            new InlineKeyboardMarkup(new[] { new[] { Styled(Button("🔄 Try again", $"search|{text}"), KeyboardButtonStyle.Primary) } }));
    }

    private static string SwapLayout(string text)
    {
        const string ru = "йцукенгшщзхъфывапролджэячсмитьбю";
        const string en = "qwertyuiop[]asdfghjkl;'zxcvbnm,.";
        var lower = text.ToLowerInvariant();
        if (lower.Any(c => ru.Contains(c)))
        {
            return string.Concat(lower.Select(c => ru.IndexOf(c) is var i and >= 0 ? en[i] : c));
        }
        if (lower.Any(c => c is >= 'a' and <= 'z'))
        {
            return string.Concat(lower.Select(c => en.IndexOf(c) is var i and >= 0 ? ru[i] : c));
        }
        return text;
    }

    private async Task<Screen> UpcomingScreenAsync(string mbid)
    {
        var setlists = await _music.SearchArtistSetlists(mbid, 1);
        var upcoming = setlists?.Setlist?.Where(s => s.IsFutureEvent()).Reverse().ToList() ?? new List<MusicSetlist>();
        var name = setlists?.Setlist?.FirstOrDefault()?.ArtistName ?? "";
        var rows = upcoming.Select(s => new[] { Button($"🔜 {FormatDate(s.EventDate)} · {s.CityName} · {s.VenueName}", $"setlist|{s.Id}|{mbid}|1") }).ToList();
        rows.Add(new[] { Button("⬅️ Artist", $"artist|{mbid}") });
        var html = upcoming.Count == 0
            ? $"📅 <b>{Encode(name)}</b>\nNo upcoming concerts on setlist.fm yet."
            : $"📅 <b>{Encode(name)}</b> · {upcoming.Count} upcoming\n<i>Added by fans on setlist.fm, tickets are not sold here.</i>";
        return new Screen(html, false, new InlineKeyboardMarkup(rows));
    }

    private async Task<Screen> ArtistScreenAsync(string mbid, string? searchText = null)
    {
        var artist = (MusicArtist)await _music.SearchArtistByMBID(mbid);
        var lastFm = artist[MusicServiceType.LastFm];

        // A plain HTML message: the Last.fm page gives the link preview with the artist photo above the text,
        // which rich messages do not have; the expandable quote works in plain HTML too.
        var html = new StringBuilder();
        html.Append($"{Flag(artist.Country)} <b>{Encode(artist.Name)}</b>\n{Encode(Describe(artist))}");
        var biography = CleanBiography(artist.Biography);
        if (biography.Length > 0)
        {
            html.Append($"\n<blockquote expandable>{Encode(biography)}</blockquote>");
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { Styled(Button("📝 Setlists", $"setlists|{mbid}|1"), KeyboardButtonStyle.Primary), Button("🎧 Top tracks", $"top|{mbid}") },
            new[] { Button("📅 Upcoming", $"upcoming|{mbid}"), Button("📖 Full biography", $"bio|{mbid}") },
            searchText == null ? new[] { Button("🔍 Another artist", "hint") } : new[] { Button("🔄 Not this artist?", $"list|{searchText}"), Button("🔍 Another artist", "hint") },
        });
        return new Screen(html.ToString(), false, keyboard, lastFm?.ExternalUrl?.ToString());
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
            .Select(s => new[] { Button(SetlistLabel(s), $"setlist|{s.Id}|{mbid}|{screen}") })
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
        return new Screen($"📝 <b>{Encode(artistName)}</b> · {setlists.Total} setlists\n🔜 upcoming · 📭 no songs on setlist.fm yet", false, new InlineKeyboardMarkup(rows));
    }

    private static string SetlistLabel(MusicSetlist setlist)
    {
        var mark = setlist.IsFutureEvent() ? "🔜 " : setlist.IsSetsExist() ? "" : "📭 ";
        return $"{mark}{FormatDate(setlist.EventDate)} · {setlist.CityName} · {setlist.VenueName}";
    }

    private async Task<Screen> SetlistScreenAsync(string setlistId, string mbid, int screen, bool asTable, bool forInline = false)
    {
        var setlist = await _music.SearchSetlist(setlistId);
        var sets = setlist.Sets?.Set ?? new List<MusicSet>();
        var songs = sets.SelectMany(set => set.Songs ?? new List<MusicSong>()).ToList();

        var html = new StringBuilder();
        html.Append($"<h3>{Encode(setlist.ArtistName)}</h3>");
        if (!string.IsNullOrEmpty(setlist.TourName))
        {
            html.Append($"<p><b>{Encode(setlist.TourName)}</b></p>");
        }
        html.Append($"<p>📍 {Encode(setlist.VenueName)}, {Encode(setlist.CityName)} ({Encode(setlist.CountryName)}) · {FormatDate(setlist.EventDate)}</p>");

        if (songs.Count == 0)
        {
            html.Append(setlist.IsFutureEvent() ? "<p>🔜 Upcoming concert: no setlist yet.</p>" : "<p>📭 Nobody has added the songs on setlist.fm yet.</p>");
        }
        else
        {
            html.Append($"<p><i>{Encode(Summary(sets, songs))}</i></p>");
            var hasNotes = songs.Any(s => Note(s).Length > 0);
            html.Append(asTable ? $"<table bordered striped><tr><th>#</th><th>Song</th>{(hasNotes ? "<th>Note</th>" : "")}</tr>" : "");
            var number = 1;
            foreach (var set in sets)
            {
                var title = set.Encore > 0 ? (set.Encore > 1 ? $"Encore {set.Encore}" : "Encore") : set.Name;
                if (!string.IsNullOrEmpty(title))
                {
                    html.Append(asTable ? $"<tr><td colspan=\"{(hasNotes ? 3 : 2)}\"><b>🔁 {Encode(title)}</b></td></tr>" : $"<h4>🔁 {Encode(title)}</h4>");
                }
                foreach (var song in set.Songs ?? new List<MusicSong>())
                {
                    // A callback button styled as a link: the song name itself opens the track.
                    var name = song.Tape
                        ? $"<i>{Encode(song.Name)}</i>"
                        : forInline
                        ? Encode(song.Name)
                        : $"<tg-button type=\"callback_data\" style=\"link\" data=\"{CallbackId($"play|{mbid}|{song.Name}")}\">{Encode(song.Name)}</tg-button>";
                    var note = Encode(Note(song));
                    html.Append(asTable
                        ? $"<tr><td>{number++}</td><td>{name}</td>{(hasNotes ? $"<td>{note}</td>" : "")}</tr>"
                        : $"<p>{number++}. {name}{(note.Length > 0 ? $" <i>{note}</i>" : "")}</p>");
                }
            }
            html.Append(asTable ? "</table>" : "");
        }
        if (!string.IsNullOrWhiteSpace(setlist.Info))
        {
            html.Append($"<blockquote>ℹ️ {Encode(setlist.Info)}</blockquote>");
        }
        var url = setlist.Url ?? $"https://www.setlist.fm/search?query={Uri.EscapeDataString(setlist.ArtistName ?? "")}";
        html.Append($"<footer>{(forInline ? "" : "Tap a song to listen · ")}Setlist from <a href=\"{url}\">setlist.fm</a></footer>");

        var rows = new List<InlineKeyboardButton[]>();
        if (songs.Count > 0)
        {
            rows.Add(new[]
            {
                new InlineKeyboardButton("📋 Copy setlist") { CopyText = new CopyTextButton { Text = CopyTextOf(setlist, songs) } },
                InlineKeyboardButton.WithSwitchInlineQuery("↗️ Send to a chat", $"s_{setlist.Id}"),
            });
            rows.Add(new[]
            {
                InlineKeyboardButton.WithUrl("🔗 Share link", await ShareUrlAsync(setlist)),
            });
        }
        rows.Add(new[] { Button("⬅️ Setlists", $"setlists|{mbid}|{screen}") });
        return new Screen(html.ToString(), true, new InlineKeyboardMarkup(rows));
    }

    private async Task<string> ShareUrlAsync(MusicSetlist setlist)
    {
        // Telegram's own share dialog: the friend gets a link that opens this setlist in the bot.
        _botUsername ??= (await _bot.GetMe()).Username;
        var link = $"https://t.me/{_botUsername}?start=s_{setlist.Id}";
        var caption = $"{setlist.ArtistName} · {FormatDate(setlist.EventDate)} · {setlist.CityName}";
        return $"https://t.me/share/url?url={Uri.EscapeDataString(link)}&text={Uri.EscapeDataString(caption)}";
    }

    private static string Summary(List<MusicSet> sets, List<MusicSong> songs)
    {
        var parts = new List<string> { $"{songs.Count(s => !s.Tape)} songs" };
        var covers = songs.Count(s => s.CoverName != null);
        var guests = songs.Count(s => s.WithName != null);
        var encores = sets.Count(s => s.Encore > 0);
        if (covers > 0) parts.Add($"{covers} {(covers == 1 ? "cover" : "covers")}");
        if (guests > 0) parts.Add($"{guests} with guests");
        if (encores > 0) parts.Add(encores == 1 ? "encore" : $"{encores} encores");
        return string.Join(" · ", parts);
    }

    private async Task<Screen> TopTracksScreenAsync(string mbid)
    {
        var tracks = (await _music.SearchTopTracks(mbid)).ToList();
        var rows = tracks.Select((t, i) => new[] { Button($"{i + 1}. {t.TrackName}", $"play|{mbid}|{t.TrackName}") }).ToList();
        rows.Add(new[] { Button("⬅️ Artist", $"artist|{mbid}") });
        return new Screen("🎧 <b>Top tracks</b> on Last.fm", false, new InlineKeyboardMarkup(rows));
    }

    private sealed record TrackMessage(string FileId, string ArtistName);

    // Prototype memory: the uploaded audio of each track message, to keep it when the message is edited.
    private readonly ConcurrentDictionary<int, TrackMessage> _tracks = new();

    private async Task SendTrackAsync(long chatId, string mbid, string song)
    {
        await _bot.SendChatAction(chatId, ChatAction.UploadDocument);
        var artist = await _music.SearchArtistByMBID(mbid);
        var track = await _music.SearchTrack(artist.Name!, song);
        if (string.IsNullOrEmpty(track.DownloadLink))
        {
            await _bot.SendMessage(chatId, $"🔇 No audio for <b>{Encode(song)}</b>.", ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new[] { new[] { Styled(Button("🗑", "close"), KeyboardButtonStyle.Danger) } }));
            return;
        }

        try
        {
            // One rich message: the player, and later the lyrics below it, edited in place.
            await using var audio = await AudioClient.GetStreamAsync(track.DownloadLink);
            var media = new InputMediaAudio(InputFile.FromStream(audio, $"{artist.Name} - {song}.mp3")) { Title = song, Performer = artist.Name };
            var sent = await _bot.SendRichMessage(chatId, TrackRichMessage(media, song, artist.Name!, lyricsHtml: null), replyMarkup: TrackKeyboard(mbid, song, artist.Name!, lyricsShown: false));
            var fileId = sent.RichMessage?.Blocks?.OfType<RichBlockAudio>().FirstOrDefault()?.Audio.FileId;
            if (fileId != null)
            {
                _tracks[sent.MessageId] = new TrackMessage(fileId, artist.Name!);
            }
            else
            {
                _logger.LogWarning("Rich track message has no audio block in the answer: lyrics cannot be added in place");
            }
        }
        catch (ApiRequestException exception)
        {
            _logger.LogWarning("Rich message with audio rejected: {Reason}. Sending a plain audio message", exception.Message);
            await using var audio = await AudioClient.GetStreamAsync(track.DownloadLink);
            await _bot.SendAudio(chatId, InputFile.FromStream(audio, $"{artist.Name} - {song}.mp3"), performer: artist.Name, title: song,
                replyMarkup: AudioKeyboard(mbid, song, artist.Name!, lyricsMessageId: null));
        }
    }

    private static InputRichMessage TrackRichMessage(InputMediaAudio media, string song, string artistName, string? lyricsHtml)
    {
        var html = $"<figure><audio src=\"tg://audio?id=track\"></audio><figcaption>{Encode(artistName)} – {Encode(song)}</figcaption></figure>{lyricsHtml}";
        return new InputRichMessage { Html = html, Media = new[] { new InputRichMessageMedia { Id = "track", Media = media } } };
    }

    private InlineKeyboardMarkup TrackKeyboard(string mbid, string song, string artistName, bool lyricsShown)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                lyricsShown ? Button("🎼 Hide lyrics", $"rhide|{mbid}|{song}") : Button("🎼 Lyrics", $"rlyrics|{mbid}|{song}"),
                new InlineKeyboardButton("📋 Copy name") { CopyText = new CopyTextButton { Text = $"{artistName} – {song}" } },
                Styled(Button("🗑", "close"), KeyboardButtonStyle.Danger),
            },
        });
    }

    private async Task ShowTrackLyricsAsync(long chatId, int messageId, string mbid, string song, bool show)
    {
        if (!_tracks.TryGetValue(messageId, out var trackMessage))
        {
            await _bot.SendMessage(chatId, "This track message is from an older prototype run. Play the song again.");
            return;
        }

        string? lyricsHtml = null;
        if (show)
        {
            var lyric = await _music.SearchLyric(trackMessage.ArtistName, song);
            lyricsHtml = !lyric.IsSuccessSearchResult || string.IsNullOrWhiteSpace(lyric.LyricText)
                ? (lyric.Instrumental ? "<p>🎼 Instrumental, no lyrics.</p>" : "<p>🎼 No lyrics found.</p>")
                : "<hr/><p>" + string.Join("<br/>", lyric.LyricText.Trim().Split('\n').Select(line => Encode(line.TrimEnd('\r')))) + "</p>";
        }

        var media = new InputMediaAudio(InputFile.FromFileId(trackMessage.FileId)) { Title = song, Performer = trackMessage.ArtistName };
        await _bot.EditMessageText(chatId, messageId, null,
            replyMarkup: TrackKeyboard(mbid, song, trackMessage.ArtistName, lyricsShown: show),
            richMessage: TrackRichMessage(media, song, trackMessage.ArtistName, lyricsHtml));
    }

    private async Task OnInlineQueryAsync(InlineQuery inlineQuery)
    {
        if (!inlineQuery.Query.StartsWith("s_", StringComparison.Ordinal))
        {
            await _bot.AnswerInlineQuery(inlineQuery.Id, Array.Empty<InlineQueryResult>(), cacheTime: 0);
            return;
        }

        // The whole setlist card goes into the chosen chat, with a button that opens it in the bot.
        var setlistId = inlineQuery.Query["s_".Length..];
        var setlist = await _music.SearchSetlist(setlistId);
        var card = await SetlistScreenAsync(setlistId, setlist.ArtistMBID!, 1, asTable: true, forInline: true);
        _botUsername ??= (await _bot.GetMe()).Username;
        var open = InlineKeyboardButton.WithUrl("🎸 Open in ConcertBuddy", $"https://t.me/{_botUsername}?start=s_{setlistId}");
        var result = new InlineQueryResultArticle(setlistId, $"{setlist.ArtistName} · {FormatDate(setlist.EventDate)}",
            new InputRichMessageContent(new InputRichMessage { Html = card.Html }))
        {
            Description = $"{setlist.VenueName}, {setlist.CityName}",
            ReplyMarkup = new InlineKeyboardMarkup(new[] { new[] { open } }),
        };
        await _bot.AnswerInlineQuery(inlineQuery.Id, new InlineQueryResult[] { result }, cacheTime: 0, isPersonal: true);
    }

    private InlineKeyboardMarkup AudioKeyboard(string mbid, string song, string artistName, int? lyricsMessageId)
    {
        var lyrics = lyricsMessageId == null
            ? Button("🎼 Lyrics", $"lyrics|{mbid}|{song}")
            : Button("🎼 Hide lyrics", $"hidelyrics|{lyricsMessageId}|{mbid}|{song}|{artistName}");
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                lyrics,
                new InlineKeyboardButton("📋 Copy name") { CopyText = new CopyTextButton { Text = $"{artistName} – {song}" } },
                Styled(Button("🗑", "close"), KeyboardButtonStyle.Danger),
            },
        });
    }

    private async Task SendLyricsAsync(long chatId, int audioMessageId, string mbid, string song)
    {
        var artist = await _music.SearchArtistByMBID(mbid);
        var lyric = await _music.SearchLyric(artist.Name!, song);
        string html;
        if (!lyric.IsSuccessSearchResult || string.IsNullOrWhiteSpace(lyric.LyricText))
        {
            html = lyric.Instrumental ? "<p>🎼 Instrumental, no lyrics.</p>" : $"<p>🎼 No lyrics found for <b>{Encode(song)}</b>.</p>";
        }
        else
        {
            var lines = string.Join("<br/>", lyric.LyricText.Trim().Split('\n').Select(line => Encode(line.TrimEnd('\r'))));
            html = $"<h4>🎼 {Encode(song)} · {Encode(artist.Name)}</h4><p>{lines}</p>";
        }

        // The lyrics answer the track message, and the track button turns into "Hide lyrics": the lyrics stay with their song.
        var sent = await _bot.SendRichMessage(chatId, new InputRichMessage { Html = html }, replyParameters: new ReplyParameters { MessageId = audioMessageId });
        await _bot.EditMessageReplyMarkup(chatId, audioMessageId, AudioKeyboard(mbid, song, artist.Name!, sent.MessageId));
    }

    private async Task<Screen> BiographyScreenAsync(string mbid)
    {
        var artist = (MusicArtist)await _music.SearchArtistByMBID(mbid);
        var lastFm = artist[MusicServiceType.LastFm] as MusicSearcher.Model.LastFm.LastFmMusicArtist;
        var paragraphs = CleanBiography(lastFm?.FullBiography).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var html = new StringBuilder($"<h3>📖 {Encode(artist.Name)}</h3>");
        foreach (var paragraph in paragraphs)
        {
            html.Append($"<p>{Encode(paragraph)}</p>");
        }
        if (lastFm?.ExternalUrl != null)
        {
            html.Append($"<footer>Biography from <a href=\"{lastFm.ExternalUrl}\">Last.fm</a>, user-contributed text under CC-BY-SA</footer>");
        }
        return new Screen(html.ToString(), true, new InlineKeyboardMarkup(new[] { new[] { Button("⬅️ Artist", $"artist|{mbid}") } }));
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
            : _bot.EditMessageText(chatId, messageId, screen.Html, ParseMode.Html, screen.Keyboard, screen.PreviewUrl == null
                ? new LinkPreviewOptions { IsDisabled = true }
                : new LinkPreviewOptions { Url = screen.PreviewUrl, ShowAboveText = true, PreferLargeMedia = true });
    }

    private InlineKeyboardButton Button(string text, string payload)
    {
        return InlineKeyboardButton.WithCallbackData(text, CallbackId(payload));
    }

    private string CallbackId(string payload)
    {
        var id = _ids.GetOrAdd(payload, _ => $"p{Interlocked.Increment(ref _nextId)}");
        _payloads[id] = payload;
        return id;
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
        var notes = new[]
        {
            song.Tape ? "📼 from tape" : null,
            song.CoverName == null ? null : $"🎸 {song.CoverName} cover",
            song.WithName == null ? null : $"👥 with {song.WithName}",
            string.IsNullOrWhiteSpace(song.Info) ? null : $"ℹ️ {song.Info}",
        };
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
