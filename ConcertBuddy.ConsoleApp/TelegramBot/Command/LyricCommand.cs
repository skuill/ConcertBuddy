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
    public class LyricCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<LyricCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<LyricCommand>>();

        public LyricCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_LYRIC}] command: [{Data.Data}]");

            var isValidQuery = await CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_LYRIC);
            if (!isValidQuery)
                return null;

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_LYRIC);
            var mbid = parameters[0];
            var trackName = String.Join(' ', parameters.Skip(1));

            var artist = await SearchHandler.SearchArtistByMBID(mbid);
            var track = await SearchHandler.SearchSpotifyTrack(artist.Name, trackName);

            var lyric = SearchHandler.SearchLyric(artist.Name, track.Name);

            if (lyric == null)
            {
                _logger.LogError($"Can't find lyric for track [{artist.Name} - {track.Name}]");

                replyText = "Something goes wrong :(! Please choose another track.";
                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton();

            return await TelegramBotClient.SendTextMessageAsync(
                chatId: Data.Message.Chat.Id,
                text: lyric,
                replyMarkup: inlineKeyboard);
        }
    }
}
