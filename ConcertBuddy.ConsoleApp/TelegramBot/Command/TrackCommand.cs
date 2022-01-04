using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class TrackCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<TrackCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<TrackCommand>>();

        public TrackCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_TRACK}] command: [{Data.Data}]");

            var isValidQuery = await CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_TRACK);
            if (!isValidQuery)
                return null;

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_TRACK);
            var mbid = parameters[0];
            var trackName = String.Join(' ', parameters.Skip(1));

            var artist = await SearchHandler.SearchArtistByMBID(mbid);
            var track = await SearchHandler.SearchSpotifyTrack(artist.Name, trackName);

            if (track == null)
            {
                _logger.LogError($"Can't find track [{artist} - {trackName}]");

                replyText = "Something goes wrong :(! Please choose another track.";
                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetLyricInlineKeyboardMenu(mbid, trackName);

            return await TelegramBotClient.SendTextMessageAsync(
                chatId: Data.Message.Chat.Id,                
                text: GetTrackMarkdown(track),
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
        }

        public static string GetTrackArtistsLinks(FullTrack track)
        {
            return string.Join(", ", track.Artists
                .Select(artist => $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>"));
        }

        public static string GetTrackMarkdown(FullTrack track)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{track.ExternalUrls["spotify"]}\">{track.Name}</a>")
                .AppendLine($"Artists: {GetTrackArtistsLinks(track)}")
                .AppendLine($"Album: <a href=\"{track.Album.ExternalUrls["spotify"]}\">{track.Album.Name}</a>")
                .AppendLine($"Duration: {TimeSpan.FromMilliseconds(track.DurationMs):m\\:ss}")
                .ToString();
        }
    }
}
