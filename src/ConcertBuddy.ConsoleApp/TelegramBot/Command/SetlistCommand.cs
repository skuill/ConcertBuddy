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
    public class SetlistCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<SetlistCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<SetlistCommand>>();

        public SetlistCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data) 
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_SETLIST}] command: [{Data.Data}]");

            var isValidQuery = CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_SETLIST, out string errorMessage);
            if (!isValidQuery)
            {
                _logger.LogError(errorMessage);
                await MessageHelper.SendAsync(TelegramBotClient, Data, errorMessage);
                return null;
            }

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_SETLIST);            
            var artistMBID = parameters[0];
            var setlistId = parameters[1];

            var setlist = await SearchHandler.SearchSetlist(setlistId);

            if (setlist == null || !setlist.IsSetsExist())
            {
                _logger.LogError($"Can't find setlist. Id: [{setlistId}], mbid: [{artistMBID}]");
                return await MessageHelper.SendUnexpectedErrorAsync(TelegramBotClient, Data.Message.Chat.Id);
            }
            
            // Save message ids for delete command 
            var messageIds = new List<int>();

            replyText = setlist.ToString();

            messageIds.Add((await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: new ReplyKeyboardRemove())).MessageId);

            var sendTextMessageTasks = new List<Task<Message>>();
            foreach (var set in setlist.Sets.Items)
            {
                replyText = $"{set.ToString()}";
                InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetTracksInlineKeyboardMenu(set, artistMBID);
                sendTextMessageTasks.Add(TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard));
            }

            
            messageIds = Task.WhenAll(sendTextMessageTasks).Result.Select(x => x.MessageId).ToList();

            InlineKeyboardMarkup deleteKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton(messageIds.ToArray());
            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: "Delete",
                                                       replyMarkup: deleteKeyboard);
        }
    }
}
