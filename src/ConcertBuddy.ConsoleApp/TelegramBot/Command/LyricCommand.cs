using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using LyricsScraperNET.Models.Responses;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class LyricCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<LyricCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<LyricCommand>>();

        public LyricCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_LYRIC}] command: [{Data.Data}]");

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_LYRIC, out string errorMessage);
            if (!isValidQuery)
            {
                _logger.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_LYRIC);
            var mbid = parameters[0];
            var trackName = String.Join(' ', parameters.Skip(1));

            var artistTast = SearchHandler.SearchArtistByMBID(mbid);

            // Return actual name from MusicBrainz. Because other platform can use additional information in name.
            var recordingTask = SearchHandler.SearchSongByName(mbid, trackName);

            var artist = await artistTast;

            if (artist == null)
            {
                _logger.LogError($"Can't find artist with mbid [{mbid}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var recording = await recordingTask;
            string trackActualName = recording != null ? recording.Title : (await SearchHandler.SearchTrack(artist.Name, trackName)).TrackName;

            var searchResult = await SearchHandler.SearchLyric(artist.Name, trackActualName);
            if (searchResult == null || searchResult.IsEmpty() || searchResult.ResponseStatusCode != ResponseStatusCode.Success)
            {
                _logger.LogError($"Can't find lyric for track [{artist.Name} - {trackActualName}]. Reason: [{searchResult?.ResponseStatusCode}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton();

            return await TelegramBotClient.SendTextMessageAsync(
                chatId: Data.Message.Chat.Id,
                text: searchResult.LyricText,
                replyMarkup: inlineKeyboard);
        }
    }
}
