using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using MusicSearcher.Model;
using MusicSearcher.MusicService;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class ArtistCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<ArtistCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<ArtistCommand>>();

        public ArtistCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data) 
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_ARTIST}] command: [{Data.Data}]");

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_ARTIST, out string errorMessage);
            if (!isValidQuery)
            {
                _logger.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            string replyText = string.Empty;

            var artistMBID = Data.GetParameterFromMessageText(CommandList.COMMAND_ARTIST);

            var artist = await SearchHandler.SearchArtistByMBID(artistMBID) as MusicArtist;

            if (artist == null)
            {
                _logger.LogError($"Can't find artist with mbid [{artistMBID}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var lastFmUrl = artist[MusicServiceType.LastFm].ExternalUrl;
            var spotifyUrl = artist[MusicServiceType.Spotify].ExternalUrl;

            replyText = $"<b>{artist.Name}</b>. ";
            if (lastFmUrl != default || spotifyUrl != default)
                replyText = replyText + "<i>Links</i>: ";
            if (lastFmUrl != default)
                replyText = replyText + "<a href=\"" + lastFmUrl.ToString() + "\">last.fm</a>, ";
            if (spotifyUrl != default)
                replyText = replyText + "<a href=\"" + spotifyUrl.ToString() + "\">spotify</a>";

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistInlineKeyboardMenu(artistMBID)
                .WithDeleteButton();

            await TelegramBotClient.AnswerCallbackQueryAsync(
                callbackQueryId: Data.Id,
                text: $"Artist: {artist.Name}");

            if (artist.ImageUri != null)
                return await TelegramBotClient.SendPhotoAsync(
                    chatId: Data.Message.Chat.Id,
                    caption: replyText,
                    photo: artist.ImageUri.ToString(),
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html);

            return await TelegramBotClient.SendTextMessageAsync(
                chatId: Data.Message.Chat.Id,
                text: replyText,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
        }
    }
}
