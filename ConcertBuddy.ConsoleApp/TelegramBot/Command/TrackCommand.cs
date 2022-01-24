using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using MusicSearcher.Model;
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

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_TRACK}] command: [{Data.Data}]");

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_TRACK, out string errorMessage);
            if (!isValidQuery)
            {
                _logger.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_TRACK);
            var mbid = parameters[0];
            var trackName = String.Join(' ', parameters.Skip(1));

            var artist = await SearchHandler.SearchArtistByMBID(mbid);
            var track = await SearchHandler.SearchTrack(artist.Name, trackName);

            if (track == null)
            {
                _logger.LogError($"Can't find track [{artist} - {trackName}]");

                replyText = "Something goes wrong :(! Please choose another track.";
                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetLyricInlineKeyboardMenu(mbid, trackName);

            var trackLink = track.DownloadLink;
            var trackMarkdown = GetTrackMarkdown(track);
            Message sendAudioResult = null;

            // Setting performer and title parameters has no effect.
            // The file name is taken from the metadata of the audio file. 
            // TODO: Allow custom track name
            if (!string.IsNullOrEmpty(trackLink))
                sendAudioResult = await TelegramBotClient.SendAudioAsync(
                    chatId: Data.Message.Chat.Id,
                    performer: artist.Name,
                    title: track.Name,
                    audio: trackLink,
                    caption: trackMarkdown,
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html);

            if (sendAudioResult != null)
                return sendAudioResult;
            _logger.LogWarning($"Can't send audio: {artist.Name} - {track.Name}");

            return await TelegramBotClient.SendTextMessageAsync(
                chatId: Data.Message.Chat.Id,                
                text: trackMarkdown,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
        }

        public static string GetTrackArtistsLinks(MusicTrack track)
        {
            return string.Join(", ", track.SpotifyTrack.Artists
                .Select(artist => $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>"));
        }

        public static string GetTrackMarkdown(MusicTrack track)
        {
            return new StringBuilder()
                .AppendLine($"<a href=\"{track.SpotifyTrack.ExternalUrls["spotify"]}\">{track.Name}</a>")
                .AppendLine($"Artists: {GetTrackArtistsLinks(track)}")
                .AppendLine($"Album: <a href=\"{track.SpotifyTrack.Album.ExternalUrls["spotify"]}\">{track.SpotifyTrack.Album.Name}</a>")
                .AppendLine($"Duration: {TimeSpan.FromMilliseconds(track.SpotifyTrack.DurationMs):m\\:ss}")
                .ToString();
        }
    }
}
