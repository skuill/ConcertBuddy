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
    public class SetlistCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_SETLIST;

        private ILogger<SetlistCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<SetlistCommand>>();

        public SetlistCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
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
            var artistMBID = parameters[0];
            var setlistId = parameters[1];

            var setlist = await SearchHandler.SearchSetlist(setlistId);

            if (setlist == null || !setlist.IsSetsExist())
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Can't find setlist. Id: [{setlistId}], mbid: [{artistMBID}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }

            // Save message ids for delete command 
            var messageIds = new List<int>();

            replyText = setlist.ToString();

            messageIds.Add((await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: replyText,
                replyMarkup: new ReplyKeyboardRemove())).MessageId);

            var sendTextMessageTasks = new List<Task<Message>>();
            foreach (var set in setlist.Sets.Items)
            {
                replyText = $"{set.ToString()}";
                InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetTracksInlineKeyboardMenu(set, artistMBID);
                sendTextMessageTasks.Add(TelegramBotClient.SendMessage(
                    chatId: Data.Message.Chat.Id,
                    text: replyText,
                    replyMarkup: inlineKeyboard));
            }

            messageIds.AddRange(Task.WhenAll(sendTextMessageTasks).Result.Select(x => x.MessageId).ToList());

            InlineKeyboardMarkup deleteKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton(messageIds.ToArray());
            return await TelegramBotClient.SendMessage(
                chatId: Data.Message.Chat.Id,
                text: "Delete",
                replyMarkup: deleteKeyboard);
        }
    }
}
