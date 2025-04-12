using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using ConcertBuddy.ConsoleApp.TelegramBot.Validation;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class BiographyCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_BIOGRAPHY;

        private ILogger<BiographyCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<BiographyCommand>>();

        public BiographyCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, CallbackQuery data)
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

            string replyText = "Sorry, but the biography of the artist was not found 😕";

            var artistMBID = Data.GetParameterFromMessageText(CurrentCommand);

            var artist = await MusicSearcherClient.SearchArtistByMBID(artistMBID);
            if (artist != null && !string.IsNullOrWhiteSpace(artist.Biography))
            {
                replyText = artist.Biography;
            }

            InlineKeyboardMarkup inlineKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton();

            return await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: replyText,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html);
        }
    }
}
