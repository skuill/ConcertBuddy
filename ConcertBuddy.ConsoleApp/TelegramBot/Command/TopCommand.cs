using ConcertBuddy.ConsoleApp.Model;
using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class TopCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<TopCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<TopCommand>>();

        public TopCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_TOP}] command: [{Data.Data}]");

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_TOP, out string errorMessage);
            if (!isValidQuery)
            {
                _logger.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_TOP);
            var parseResult = Enum.TryParse<SearchType>(parameters[0], ignoreCase: true, out SearchType searchType);
            if (!parseResult || searchType == SearchType.Unknown)
            {
                _logger.LogError($"Can't parse [{parameters[0]}] parameter in [{CommandList.COMMAND_TOP}] command");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            string mbid = parameters[1];

            var result = searchType switch
            {
                SearchType.Artist => null,
                SearchType.Album => null,
                SearchType.Track => await ProcessTracks(mbid),
                SearchType.Unknown => null,
                _ => null
            };

            return result;
        }

        private async Task<Message> ProcessTracks(string mbid)
        {
            // !!!SWITCH TO SEARCH TOP TRACKS BY MBID!!!
            //var artist = await SearchHandler.SearchArtistByMBID(mbid);
            //if (artist == null)
            //{
            //    _logger.LogError($"Can't find artist with mbid [{mbid}]");
            //    return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            //}

            var topTracks = await SearchHandler.SearchTopTracks(mbid);

            if (topTracks == null || !topTracks.Any())
            {
                _logger.LogError($"Can't find top tracks for artist with mbid [{mbid}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var replyText = $"Top {topTracks.Count()} tracks 🎵. Please select a track:";
            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper
                .GetTracksInlineKeyboardMenu(topTracks.ToArray(), mbid)
                .WithDeleteButton();
            
            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                   text: replyText,
                                                   replyMarkup: inlineKeyboard);
        }
    }
}
