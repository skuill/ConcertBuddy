using Microsoft.Extensions.Logging;
using MusicSearcher.Model;
using SetlistFmAPI.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot
{
    public class BotHandlers : IBotHandlers
    {
        private readonly ILogger<IBotHandlers> _logger;
        private readonly ISearchHandler _searchHandler;

        private const string COMMAND_SEARCH = "/search";
        private const string COMMAND_ARTIST = "/artist";
        private const string COMMAND_BIOGRAPHY = "/biography";
        private const string COMMAND_SETLISTS = "/setlists";
        private const string COMMAND_SETLIST = "/setlist";
        private const string COMMAND_TRACK = "/track";

        // Command with mbid. Example: /artist b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        private const string CALLBACK_DATA_FORMAT_ARTIST = $"{COMMAND_ARTIST} {{0}}";
        // Command with mbid. Example: /biography b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        private const string CALLBACK_DATA_FORMAT_BIOGRAPHY = $"{COMMAND_BIOGRAPHY} {{0}}";
        // Command with mbid. Example: /setlists b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        private const string CALLBACK_DATA_FORMAT_SETLISTS = $"{COMMAND_SETLISTS} {{0}}";
        // Command with mbid. Example: /setlists b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        private const string CALLBACK_DATA_FORMAT_SETLIST = $"{COMMAND_SETLIST} {{0}} {{1}}";
        // Command with mbid. Example: /setlists b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        private const string CALLBACK_DATA_FORMAT_TRACK = $"{COMMAND_TRACK} {{0}}";

        public BotHandlers(ILogger<IBotHandlers> logger, ISearchHandler searchHandler)
        {
            _logger = logger;
            _searchHandler = searchHandler;
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(ErrorMessage);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                // UpdateType.Unknown:
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                // UpdateType.InlineQuery:
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private async Task<Message> SearchCommandHandler(ITelegramBotClient botClient, Message message)
        {
            _logger.LogDebug($"Handle search command: [{message.Text}]");

            var splitMessage = message.GetSplitMessageText();
            string replyText = string.Empty;

            if (splitMessage.Count() == 1)
            {
                replyText = $"Please pass artist's name as a parameter. For example: [{COMMAND_SEARCH} The Beatles]";
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            var artistName = message.GetParameterFromMessageText(COMMAND_SEARCH);

            var artists = await _searchHandler.SearchArtistsByName(artistName);

            if (artists == null || !artists.Any())
            {
                _logger.LogError($"Can't find artist [{artistName}]");

                replyText = "Something goes wrong :(! Please try to find another artist. ";
                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            if (artists.Count() == 1)
            {
                // TODO: answer immediately without inlineKeyboardButtons choice.
            }

            InlineKeyboardMarkup inlineKeyboard = GetArtistsWithScoreInlineKeyboard(artists);

            replyText = "Choose the right artist:";
            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: replyText,
                                                        replyMarkup: inlineKeyboard);
        }

        private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            _logger.LogInformation($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;
            var action = message.GetSplitMessageText()[0] switch
            {
                $"{COMMAND_SEARCH}" => SearchCommandHandler(botClient, message),
                _ => Usage(botClient, message)
            };
            Message sentMessage = await action;
            _logger.LogInformation($"The message was sent with id: {sentMessage?.MessageId}");

        }

        private static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
        {
            const string usage = "Usage:\n" +
                                 $"{COMMAND_SEARCH}   - search artist's biography, setlists and lyrics for songs\n";

            return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: usage,
                                                        replyMarkup: new ReplyKeyboardRemove());
        }

        private async Task<Message> ArtistCommandHandler(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            _logger.LogDebug($"Handle artist command: [{callbackQuery.Data}]");

            var isValidQuery = await ValidateCallbackQueryData(botClient, callbackQuery, COMMAND_ARTIST);
            if (!isValidQuery)
                return null;

            string replyText = string.Empty;

            var mbid = callbackQuery.GetParameterFromMessageText(COMMAND_ARTIST);

            var artist = await _searchHandler.SearchArtistByMBID(mbid);

            if (artist.ImageUri != null)
            {
                await botClient.SendPhotoAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    photo: artist.ImageUri.ToString());
            }

            replyText = $"<b>{artist.Name}</b>. ";
            if (artist.LastFmUrl != null || artist.SpotifyUrl != null)
                replyText = replyText + "<i>Links</i>: ";
            if (artist.LastFmUrl != null)
                replyText = replyText + "<a href=\"" + artist.LastFmUrl.ToString() + "\">last.fm</a>, ";
            if (artist.SpotifyUrl != null)
                replyText = replyText + "<a href=\"" + artist.SpotifyUrl.ToString() + "\">spotify</a>";

            InlineKeyboardMarkup inlineKeyboard = GetArtistInlineKeyboardMenu(mbid);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Artist: {artist.Name}");

            return await botClient.SendTextMessageAsync(chatId: callbackQuery.Message.Chat.Id,
                                                        text: replyText,
                                                        replyMarkup: inlineKeyboard,
                                                        parseMode: ParseMode.Html);
        }

        private async Task<Message> BiographyCommandHandler(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var isValidQuery = await ValidateCallbackQueryData(botClient, callbackQuery, COMMAND_BIOGRAPHY);
            if (!isValidQuery)
                return null;

            string replyText = "Sorry, but the biography of this artist was not found ☹️";

            var mbid = callbackQuery.GetParameterFromMessageText(COMMAND_BIOGRAPHY);
            var artist = await _searchHandler.SearchArtistByMBID(mbid);            
            if (artist.Biography != null)
            {
                replyText = artist.Biography;
            }

            InlineKeyboardMarkup inlineKeyboard = GetArtistInlineKeyboardMenu(mbid);

            return await botClient.SendTextMessageAsync(chatId: callbackQuery.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard,
                                                       parseMode: ParseMode.Html);
        }

        private async Task<Message> SetlistsCommandHandler(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var isValidQuery = await ValidateCallbackQueryData(botClient, callbackQuery, COMMAND_SETLISTS);
            if (!isValidQuery)
                return null;

            var replyText = "Please select a setlist:";

            var mbid = callbackQuery.GetParameterFromMessageText(COMMAND_SETLISTS);
            var setlists = await _searchHandler.SearchArtistSetlists(mbid);

            if (setlists == null || setlists.IsEmpty())
            {
                replyText = $"Can't find any setlist for artist MBID {mbid}";

                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"{replyText}");

                return await botClient.SendTextMessageAsync(chatId: callbackQuery.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }
            
            InlineKeyboardMarkup inlineKeyboard = GetSetlistsInlineKeyboardMenu(setlists.Items);

            return await botClient.SendTextMessageAsync(chatId: callbackQuery.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard,
                                                       parseMode: ParseMode.Html);
        }

        // Process Inline Keyboard callback data
        private async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var action = callbackQuery.GetSplitMessageText()[0] switch
            {
                $"{COMMAND_ARTIST}" => ArtistCommandHandler(botClient, callbackQuery),
                $"{COMMAND_BIOGRAPHY}" => BiographyCommandHandler(botClient, callbackQuery),
                $"{COMMAND_SETLISTS}" => SetlistsCommandHandler(botClient, callbackQuery),
                _ => Usage(botClient, callbackQuery.Message)
            };
        }

        private Task BotOnChosenInlineResultReceived(ITelegramBotClient botClient, ChosenInlineResult chosenInlineResult)
        {
            _logger.LogInformation($"Received inline result: {chosenInlineResult.ResultId}");
            return Task.CompletedTask;
        }

        private Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            _logger.LogInformation($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }

        private async Task<bool> ValidateCallbackQueryData(ITelegramBotClient botClient, CallbackQuery callbackQuery, string command = "/SOME_COMMAND")
        {
            var splitMessage = callbackQuery.GetSplitMessageText();
            string replyText = string.Empty;

            if (splitMessage.Count() == 1)
            {
                replyText = $"Please pass artist's MBID as a parameter. For example: [{command} b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d]";

                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: replyText);

                await botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: replyText);

                return false;
            }
            return true;
        }

        private InlineKeyboardMarkup GetSetlistsInlineKeyboardMenu(IEnumerable<Setlist> setlists)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            foreach (var setlist in setlists)
            {
                string callbackText = $"{setlist.ToString()}";
                string callbackData = string.Format(CALLBACK_DATA_FORMAT_SETLIST, setlist.Artist.MBID, setlist.Id);
                inlineKeyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData(callbackText, callbackData) });
            }
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        private InlineKeyboardMarkup GetArtistsWithScoreInlineKeyboard(IEnumerable<MusicArtist> artists)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            int counter = 1;
            foreach (var artist in artists)
            {
                string callbackText = $"{counter++}. {artist.Name}   [score:  {artist.Score}%]";
                string callbackData = string.Format(CALLBACK_DATA_FORMAT_ARTIST, artist.MBID);
                inlineKeyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData(callbackText, callbackData) });
            }
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        private InlineKeyboardMarkup GetArtistInlineKeyboardMenu(string mbid)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            inlineKeyboardButtons.Add(new[] {
                InlineKeyboardButton.WithCallbackData("🎓 Biography", string.Format(CALLBACK_DATA_FORMAT_BIOGRAPHY, mbid)),
                InlineKeyboardButton.WithCallbackData("📝 Setlists", string.Format(CALLBACK_DATA_FORMAT_SETLISTS, mbid)),
            });
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }
    }
}
