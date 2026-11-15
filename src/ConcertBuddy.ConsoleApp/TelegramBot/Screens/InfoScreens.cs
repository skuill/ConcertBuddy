using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// The start menu, help and about. Each one leads to the others, so no screen is a dead end.
/// </summary>
public sealed class InfoScreens
{
    public const string ProjectUrl = "https://github.com/skuill/ConcertBuddy";

    private readonly CallbackCodec _codec;

    public InfoScreens(CallbackCodec codec)
    {
        _codec = codec;
    }

    public static string Version => typeof(InfoScreens).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public Screen Start(string? firstName)
    {
        var greeting = string.IsNullOrWhiteSpace(firstName) ? "👋 Hi!" : $"👋 Hi, {ScreenText.Html(firstName)}!";
        var html = $"{greeting}\n\n"
            + "🎸 <b>ConcertBuddy</b> shows what artists play live: setlists of their concerts, every song to listen to and its lyrics.\n\n"
            + "Send an artist or band name, or try one of these:";
        var rows = SearchScreens.Examples
            .Select(name => InlineKeyboardButton.WithCallbackData(name, _codec.Encode(CallbackActions.NewSearch, "0", name)))
            .Chunk(2)
            .Append(new[] { HelpButton(), AboutButton() });
        return new Screen(html, new InlineKeyboardMarkup(rows));
    }

    public Screen Help()
    {
        var html = "❓ <b>How to use ConcertBuddy</b>\n\n"
            + "🔍 <b>Search:</b> send an artist or band name, or <code>/search Metallica</code>.\n"
            + "📝 <b>Setlists:</b> open the artist, then Setlists, then a concert.\n"
            + "🎧 <b>Listen:</b> tap a song in a setlist or in Top tracks.\n"
            + "🎼 <b>Lyrics:</b> under every track.\n"
            + "📖 <b>Biography:</b> on the artist card.";
        return new Screen(html, new InlineKeyboardMarkup(new[] { new[] { MenuButton(), AboutButton() } }));
    }

    public Screen About()
    {
        var html = $"ℹ️ <b>ConcertBuddy</b> {Version}\n\n"
            + "Data: <a href=\"https://musicbrainz.org\">MusicBrainz</a>, <a href=\"https://www.setlist.fm\">setlist.fm</a>, "
            + "<a href=\"https://www.last.fm\">Last.fm</a>, Yandex Music, lyrics via <a href=\"https://github.com/skuill/LyricsScraperNET\">LyricsScraperNET</a>.\n"
            + $"Open source: <a href=\"{ProjectUrl}\">github.com/skuill/ConcertBuddy</a>\n\n"
            + "💛 The bot is free. If it helps you, you can support it:";
        var rows = new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("☕ Buy me a coffee", "https://www.buymeacoffee.com/skuill"),
                InlineKeyboardButton.WithUrl("💳 PayPal", "https://paypal.me/VitalyKvashin"),
            },
            new[] { InlineKeyboardButton.WithUrl("🪙 Crypto", $"{ProjectUrl}#support") },
            new[] { MenuButton(), HelpButton() },
        };
        return new Screen(html, new InlineKeyboardMarkup(rows));
    }

    /// <summary>
    /// "/search" without a name.
    /// </summary>
    public static Screen SearchHint()
    {
        return new Screen("🔍 Add a name: <code>/search Metallica</code>, or just send the name.", InlineKeyboardMarkup.Empty());
    }

    private InlineKeyboardButton MenuButton() => InlineKeyboardButton.WithCallbackData("🏠 Menu", _codec.Encode(CallbackActions.Menu));

    private InlineKeyboardButton HelpButton() => InlineKeyboardButton.WithCallbackData("❓ Help", _codec.Encode(CallbackActions.Help));

    private InlineKeyboardButton AboutButton() => InlineKeyboardButton.WithCallbackData("ℹ️ About", _codec.Encode(CallbackActions.About));
}
