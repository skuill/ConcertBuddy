using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Model.Abstract;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// A song of a setlist or of the top tracks: a new track message with the audio. The setlist stays where it is.
/// </summary>
public class PlayCommand : AbstractCommand<Message?, CallbackQuery>
{
    /// <summary>
    /// Telegram shows up to 200 characters in a toast.
    /// </summary>
    private const int MaxToastSongLength = 150;

    private readonly ScreenSender _sender;
    private readonly TrackScreens _screens;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PlayCommand> _logger;
    private readonly CallbackPayload _payload;

    public PlayCommand(
        IMusicSearcherClient musicSearcherClient,
        ITelegramBotClient telegramBotClient,
        ScreenSender sender,
        TrackScreens screens,
        IHttpClientFactory httpClientFactory,
        ILogger<PlayCommand> logger,
        CallbackQuery data,
        CallbackPayload payload)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _sender = sender;
        _screens = screens;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _payload = payload;
    }

    public override async Task<Message?> ExecuteAsync()
    {
        var arguments = _payload.Split(2);
        if (arguments == null || !Guid.TryParse(arguments[0], out _) || arguments[1].Length == 0 || Data.Message == null)
        {
            await OutdatedCommand.AnswerAsync(TelegramBotClient, Data);
            Result = CommandResult.Outdated;
            AnsweredCallback = true;
            return null;
        }

        var (mbid, song) = (arguments[0], arguments[1]);
        var chatId = Data.Message.Chat.Id;

        // At once: the search and the upload take seconds.
        await TelegramBotClient.AnswerCallbackQuery(Data.Id, $"⏳ Loading {ScreenText.Shorten(song, MaxToastSongLength)}…");
        AnsweredCallback = true;
        await TelegramBotClient.SendChatAction(chatId, ChatAction.UploadDocument);

        MusicArtistBase artist;
        try
        {
            artist = await MusicSearcherClient.SearchArtistByMBID(mbid);
        }
        catch (MusicServiceUnavailableException exception)
        {
            Result = CommandResult.Unavailable;
            return await _sender.SendAsync(chatId, _screens.Unavailable(exception.Service, mbid, song));
        }

        if (string.IsNullOrEmpty(artist.Name))
        {
            Result = CommandResult.NotFound;
            var gone = new Screen("🤷 MusicBrainz does not have this artist any more.", new InlineKeyboardMarkup(new[] { new[] { CommonScreens.DeleteButton() } }));
            return await _sender.SendAsync(chatId, gone);
        }

        var track = await MusicSearcherClient.SearchTrack(artist.Name, song);
        await using var audioStream = await TryDownloadAsync(track.DownloadLink);
        var audio = audioStream == null
            ? null
            : new InputMediaAudio(InputFile.FromStream(audioStream, TrackScreens.FileName(artist.Name, song))) { Title = song, Performer = artist.Name };

        var sent = await _sender.SendAsync(chatId, _screens.Track(mbid, artist.Name, song, audio, lyricsHtml: null));
        Result = audio == null ? CommandResult.NotFound : CommandResult.Found;

        if (_payload.Action == CallbackActions.PlayAgain)
        {
            await TryDeleteAsync(chatId, Data.Message.MessageId);
        }
        return sent;
    }

    private async Task<Stream?> TryDownloadAsync(string? link)
    {
        if (string.IsNullOrEmpty(link))
        {
            return null;
        }

        try
        {
            // Downloaded by the bot, not by Telegram from the link: so the file gets a readable name "Artist - Song.mp3".
            var response = await _httpClientFactory.CreateClient(ServiceCollectionExtensions.AudioClientName).GetAsync(link, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Can't download the audio of a track, sending it without audio");
            return null;
        }
    }

    private async Task TryDeleteAsync(long chatId, int messageId)
    {
        try
        {
            await TelegramBotClient.DeleteMessage(chatId, messageId);
        }
        catch (ApiRequestException exception)
        {
            // Already deleted by the user, or older than 48 hours: then it just stays in the chat.
            _logger.LogDebug(exception, "Can't delete the busy message {MessageId}", messageId);
        }
    }
}
