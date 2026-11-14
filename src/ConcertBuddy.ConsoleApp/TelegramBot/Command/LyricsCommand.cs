using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// Shows or hides the lyrics in the track message. The audio stays: its file id and performer are read from the
/// message itself, so the buttons keep working after a restart.
/// </summary>
public class LyricsCommand : ScreenCallbackCommand
{
    private readonly TrackScreens _screens;

    public LyricsCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, ScreenSender sender, TrackScreens screens, CallbackQuery data, CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, sender, data, payload)
    {
        _screens = screens;
    }

    protected override int ArgumentCount => 2;

    protected override string? AnswerText => Show ? "⏳ Looking for the lyrics…" : null;

    private bool Show => Payload.Action == CallbackActions.ShowLyrics;

    protected override Func<Task<ScreenResult>>? Prepare(string[] arguments)
    {
        var (mbid, song) = (arguments[0], arguments[1]);
        if (!Guid.TryParse(mbid, out _) || song.Length == 0)
        {
            return null;
        }

        return () => BuildAsync(mbid, song);
    }

    private async Task<ScreenResult> BuildAsync(string mbid, string song)
    {
        var playing = Data.Message?.RichMessage?.Blocks?.OfType<RichBlockAudio>().FirstOrDefault()?.Audio;
        var audio = playing == null ? null : new InputMediaAudio(InputFile.FromFileId(playing.FileId)) { Title = song, Performer = playing.Performer };

        var artistName = playing?.Performer;
        if (string.IsNullOrEmpty(artistName))
        {
            try
            {
                artistName = (await MusicSearcherClient.SearchArtistByMBID(mbid)).Name ?? string.Empty;
            }
            catch (MusicServiceUnavailableException exception)
            {
                var busy = $"<p>⚠️ {ScreenText.Html(exception.Service)} is busy right now, tap Lyrics again in a minute.</p>";
                var track = _screens.Track(mbid, string.Empty, song, audio, lyricsHtml: null);
                return new ScreenResult(track with { Html = track.Html + busy }, CommandResult.Unavailable);
            }
        }

        if (!Show)
        {
            return new ScreenResult(_screens.Track(mbid, artistName, song, audio, lyricsHtml: null), CommandResult.Found);
        }

        var (lyricsHtml, result) = await _screens.LyricsAsync(mbid, artistName, song);
        return new ScreenResult(_screens.Track(mbid, artistName, song, audio, lyricsHtml), result);
    }
}
