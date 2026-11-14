using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using MusicSearcher;
using MusicSearcher.Model.Abstract;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// A track: one rich message with the audio player and the lyrics shown and hidden in place; and the top tracks of an artist.
/// </summary>
public sealed class TrackScreens
{
    /// <summary>
    /// The id of the audio in the rich message HTML (<c>tg://audio?id=track</c>).
    /// </summary>
    public const string AudioMediaId = "track";

    /// <summary>
    /// Lyrics are cut far below the rich message limit of 32,768 characters.
    /// </summary>
    public const int MaxLyricsLength = 20_000;

    private readonly IMusicSearcherClient _musicSearcher;
    private readonly CallbackCodec _codec;

    public TrackScreens(IMusicSearcherClient musicSearcher, CallbackCodec codec)
    {
        _musicSearcher = musicSearcher;
        _codec = codec;
    }

    /// <summary>
    /// The track message. The audio is a new upload or the file id of the message, null for a track without audio;
    /// the lyrics part is null while the lyrics are hidden. Without an artist name the title is the song alone.
    /// </summary>
    public Screen Track(string mbid, string artistName, string song, InputMediaAudio? audio, string? lyricsHtml)
    {
        var name = artistName.Length == 0 ? song : $"{artistName} – {song}";
        var title = ScreenText.Html(name);
        var html = audio == null
            ? $"<h3>{title}</h3><p>🔇 No audio for this song.</p>"
            : $"<figure><audio src=\"tg://audio?id={AudioMediaId}\"></audio><figcaption>{title}</figcaption></figure>";
        var media = audio == null ? null : new[] { new InputRichMessageMedia { Id = AudioMediaId, Media = audio } };

        var lyrics = lyricsHtml == null
            ? InlineKeyboardButton.WithCallbackData("🎼 Lyrics", _codec.Encode(CallbackActions.ShowLyrics, mbid, song))
            : InlineKeyboardButton.WithCallbackData("🎼 Hide lyrics", _codec.Encode(CallbackActions.HideLyrics, mbid, song));
        var copy = new InlineKeyboardButton("📋 Copy name") { CopyText = new CopyTextButton { Text = StringHelper.SubstringByByteLength(name, SetlistScreens.MaxCopyTextBytes) } };
        var keyboard = new InlineKeyboardMarkup(new[] { new[] { lyrics, copy, CommonScreens.DeleteButton() } });
        return new Screen(html + lyricsHtml, keyboard, IsRich: true, Media: media);
    }

    /// <summary>
    /// The lyrics part of the track message: the text, "instrumental" or "not found".
    /// </summary>
    public async Task<(string Html, CommandResult Result)> LyricsAsync(string mbid, string artistName, string song)
    {
        // MusicBrainz knows the exact title ("Seek & Destroy" for "Seek and Destroy"), the lyrics sites find it better.
        var recording = await _musicSearcher.SearchRecordByName(mbid, song);
        var lyric = await _musicSearcher.SearchLyric(artistName, recording?.Title ?? song);
        if (lyric.Instrumental)
        {
            return ("<p>🎼 Instrumental, no lyrics.</p>", CommandResult.Found);
        }
        if (!lyric.IsSuccessSearchResult || string.IsNullOrWhiteSpace(lyric.LyricText))
        {
            return ("<p>🎼 No lyrics found for this song.</p>", CommandResult.NotFound);
        }

        var text = ScreenText.Shorten(lyric.LyricText.Trim(), MaxLyricsLength);
        var lines = text.Split('\n').Select(line => ScreenText.Html(line.TrimEnd('\r')));
        return ($"<hr/><p>{string.Join("<br/>", lines)}</p>", CommandResult.Found);
    }

    public async Task<ScreenResult> TopTracksAsync(string mbid)
    {
        List<MusicTrackBase> tracks;
        try
        {
            tracks = (await _musicSearcher.SearchTopTracks(mbid))?.ToList() ?? new List<MusicTrackBase>();
        }
        catch (MusicServiceUnavailableException exception)
        {
            return CommonScreens.Unavailable(exception.Service, _codec.Encode(CallbackActions.TopTracks, mbid));
        }

        var back = new[] { InlineKeyboardButton.WithCallbackData("⬅️ Artist", _codec.Encode(CallbackActions.Artist, mbid, "0", string.Empty)) };
        var named = tracks.Where(track => !string.IsNullOrWhiteSpace(track.TrackName)).ToList();
        if (named.Count == 0)
        {
            return new ScreenResult(new Screen("📻 No top tracks of this artist on Last.fm yet.", new InlineKeyboardMarkup(new[] { back })), CommandResult.NotFound);
        }

        var rows = named
            .Select((track, index) => new[] { InlineKeyboardButton.WithCallbackData($"{index + 1}. {track.TrackName}", PlayData(mbid, track.TrackName!)) })
            .Append(back);
        return new ScreenResult(new Screen("📻 <b>Top tracks</b> on Last.fm, tap one to listen:", new InlineKeyboardMarkup(rows)), CommandResult.Found);
    }

    public string PlayData(string mbid, string song)
    {
        return _codec.Encode(CallbackActions.Play, mbid, song);
    }

    /// <summary>
    /// "MusicBrainz is busy" as a new message under the setlist, with Try again that replaces it with the track.
    /// </summary>
    public Screen Unavailable(string service, string mbid, string song)
    {
        return CommonScreens.Unavailable(service, _codec.Encode(CallbackActions.PlayAgain, mbid, song)).Screen;
    }

    public static string FileName(string artistName, string song)
    {
        var name = $"{artistName} - {song}";
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).ToHashSet();
        var clean = new StringBuilder();
        foreach (var letter in name)
        {
            clean.Append(invalid.Contains(letter) ? '_' : letter);
        }
        return $"{ScreenText.Shorten(clean.ToString().Trim(), 120)}.mp3";
    }
}
