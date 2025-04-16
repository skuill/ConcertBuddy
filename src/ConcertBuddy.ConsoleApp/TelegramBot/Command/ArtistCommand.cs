using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.MusicService;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class ArtistCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_ARTIST;

        private ILogger<ArtistCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<ArtistCommand>>();

        public ArtistCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, CallbackQuery data)
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
                _logger?.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            string replyText = string.Empty;

            var artistMBID = Data.GetParameterFromMessageText(CurrentCommand);

            var artist = await MusicSearcherClient.SearchArtistByMBID(artistMBID) as MusicArtist;

            if (artist == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find artist by mbid [{artistMBID}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var lastFmUrl = artist[MusicServiceType.LastFm]?.ExternalUrl;
            var spotifyUrl = artist[MusicServiceType.Spotify]?.ExternalUrl;

            replyText = $"<b>{artist.Name}</b>. ";
            if (lastFmUrl != default || spotifyUrl != default)
                replyText = replyText + "<i>Links</i>: ";
            if (lastFmUrl != default)
                replyText = replyText + "<a href=\"" + lastFmUrl.ToString() + "\">last.fm</a>, ";
            if (spotifyUrl != default)
                replyText = replyText + "<a href=\"" + spotifyUrl.ToString() + "\">spotify</a>";

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistInlineKeyboardMenu(artistMBID)
                .WithDeleteButton();

            await TelegramBotClient.AnswerCallbackQuery(
                callbackQueryId: Data.Id,
                text: $"Artist: {artist.Name}");

            if (artist.ImageUri != null)
                return await TelegramBotClient.SendPhoto(
                    chatId: Data.Message.Chat.Id,
                    caption: replyText,
                    photo: InputFile.FromUri(artist.ImageUri),
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html);

            return await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: replyText,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
        }
    }
}
