﻿using ConcertBuddy.ConsoleApp.Search;
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
    public class LyricCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_LYRIC;

        private ILogger<LyricCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<LyricCommand>>();

        public LyricCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
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

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CurrentCommand);
            var mbid = parameters[0];
            var trackName = String.Join(' ', parameters.Skip(1));

            var artistTast = SearchHandler.SearchArtistByMBID(mbid);

            // Return actual name from MusicBrainz. Because other platform can use additional information in name.
            var recordingTask = SearchHandler.SearchSongByName(mbid, trackName);

            var artist = await artistTast;

            if (artist == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find artist with mbid [{mbid}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            if (string.IsNullOrWhiteSpace(artist.Name))
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find artist name by mbid [{mbid}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var recording = await recordingTask;
            string? trackActualName = recording != null 
                ? recording.Title 
                : ((await SearchHandler.SearchTrack(artist.Name, trackName)).TrackName ?? null);

            if (string.IsNullOrWhiteSpace(trackActualName))
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find actual track by provided name: [{artist.Name} - {trackName}].");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            var searchResult = await SearchHandler.SearchLyric(artist.Name, trackActualName);
            if (searchResult == null
                || searchResult.ResponseStatusCode != ResponseStatusCode.Success)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find lyric for track [{artist.Name} - {trackActualName}]. Reason: [{searchResult?.ResponseStatusCode}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            if (searchResult.ResponseStatusCode == ResponseStatusCode.Success
                && searchResult.Instrumental)
            {
                replyText = "This song is an instrumental";
            }
            else
            {
                replyText = searchResult.LyricText;
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton();

            return await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: replyText,
                replyMarkup: inlineKeyboard);
        }
    }
}
