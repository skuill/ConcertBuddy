using ConcertBuddy.ConsoleApp.Model;
using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class TopCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_TOP;

        private ILogger<TopCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<TopCommand>>();

        public TopCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(musicSearcherClient, telegramBotClient, data)
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
            var parseResult = Enum.TryParse<SearchType>(parameters[0], ignoreCase: true, out SearchType searchType);
            if (!parseResult || searchType == SearchType.Unknown)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't parse [{parameters[0]}] parameter");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            string mbid = parameters[1];

            return searchType switch
            {
                SearchType.Artist => null,
                SearchType.Album => null,
                SearchType.Track => await ProcessTracks(mbid),
                SearchType.Unknown => null,
                _ => null
            };
        }

        private async Task<Message> ProcessTracks(string mbid)
        {
            // !!!SWITCH TO SEARCH TOP TRACKS BY MBID!!!
            //var artist = await MusicSearcherClient.SearchArtistByMBID(mbid);
            //if (artist == null)
            //{
            //    _logger?.LogError($"Can't find artist with mbid [{mbid}]");
            //    return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            //}

            var topTracks = await MusicSearcherClient.SearchTopTracks(mbid);

            if (topTracks == null || !topTracks.Any())
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find top tracks for artist with mbid [{mbid}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var replyText = $"Top {topTracks.Count()} tracks 🎵. Please select a track:";
            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper
                .GetTracksInlineKeyboardMenu(topTracks.ToArray(), mbid)
                .WithDeleteButton();

            return await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: replyText,
                replyMarkup: inlineKeyboard);
        }
    }
}
