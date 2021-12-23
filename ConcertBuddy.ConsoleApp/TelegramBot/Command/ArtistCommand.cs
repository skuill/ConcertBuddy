﻿using ConcertBuddy.ConsoleApp.Search;
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
    public class ArtistCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<ArtistCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<ArtistCommand>>();

        public ArtistCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data) 
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            _logger.LogDebug($"Handle artist command: [{Data.Data}]");

            var isValidQuery = await CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_ARTIST);
            if (!isValidQuery)
                return null;

            string replyText = string.Empty;

            var mbid = Data.GetParameterFromMessageText(CommandList.COMMAND_ARTIST);

            var artist = await SearchHandler.SearchArtistByMBID(mbid);

            replyText = $"<b>{artist.Name}</b>. ";
            if (artist.LastFmUrl != null || artist.SpotifyUrl != null)
                replyText = replyText + "<i>Links</i>: ";
            if (artist.LastFmUrl != null)
                replyText = replyText + "<a href=\"" + artist.LastFmUrl.ToString() + "\">last.fm</a>, ";
            if (artist.SpotifyUrl != null)
                replyText = replyText + "<a href=\"" + artist.SpotifyUrl.ToString() + "\">spotify</a>";

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetArtistInlineKeyboardMenu(mbid)
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