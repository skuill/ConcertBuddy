using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class TrackCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_TRACK;

        private ILogger<TrackCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<TrackCommand>>();

        public TrackCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message?> ExecuteAsync()
        {
            _logger?.LogDebug($"Handle [{CurrentCommand}] command: [{Data.Data}]");

            if (Data == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Unexpected case. [Data] field is null.");
                return null;
            }
            if (Data!.Message == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Unexpected case. [Data.Message] field is null.");
            }

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CurrentCommand, out string errorMessage);
            if (!isValidQuery)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Error: {errorMessage}");
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CurrentCommand);
            var mbid = parameters[0];
            var trackName = String.Join(' ', parameters.Skip(1));

            var artist = await SearchHandler.SearchArtistByMBID(mbid);
            if (artist == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find artist with mbid [{mbid}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            if (string.IsNullOrWhiteSpace(artist.Name))
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find artist name by mbid [{mbid}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var track = await SearchHandler.SearchTrack(artist.Name, trackName);

            if (track == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find track [{artist} - {trackName}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetLyricInlineKeyboardMenu(mbid, trackName);

            var trackLink = track.DownloadLink;
            var trackMarkdown = track.GetTrackMarkdown();

            // Setting performer and title parameters has no effect.
            // The file name is taken from the metadata of the audio file. 
            // TODO: Allow custom track name
            if (!string.IsNullOrEmpty(trackLink))
            {
                var sendAudioResult = await TelegramBotClient.SendAudio(
                    chatId: Data.Message.Chat.Id,
                    performer: artist.Name,
                    title: track.TrackName,
                    audio: InputFile.FromString(trackLink),
                    caption: trackMarkdown,
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html);

                if (sendAudioResult != null)
                    return sendAudioResult;
            }

            _logger?.LogWarning($"Command: [{CurrentCommand}]. Can't send audio: {artist.Name} - {track.TrackName}");

            return await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: trackMarkdown,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
        }
    }
}
