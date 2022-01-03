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

        public async Task<Message> Execute()
        {
            _logger.LogDebug($"Handle search command: [{Data.Data}]");

            var isValidQuery = await CallbackQueryValidation.Validate(TelegramBotClient, Data, CommandList.COMMAND_SETLIST);
            if (!isValidQuery)
                return null;

            var replyText = string.Empty;

            var parameters = Data.GetParametersFromMessageText(CommandList.COMMAND_SETLIST);            
            var artistMBID = parameters[0];
            var setlistId = parameters[1];

            var setlist = await SearchHandler.SearchSetlist(setlistId);

            if (setlist == null || !setlist.IsSetsExist())
            {
                _logger.LogError($"Can't find setlist. Id: [{setlistId}], mbid: [{artistMBID}]");

                replyText = "Something goes wrong :(! Please try another setlist..";
                return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                            text: replyText,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }

            List<int> messageIds = new List<int>();

            replyText = setlist.ToString();

            messageIds.Add((await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: new ReplyKeyboardRemove())).MessageId);

            foreach (var set in setlist.Sets.Items)
            {
                replyText = $"{set.ToString()}";
                InlineKeyboardMarkup inlineKeyboard = InlineKeyboardHelper.GetTracksInlineKeyboardMenu(set, artistMBID);
                messageIds.Add((await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: replyText,
                                                       replyMarkup: inlineKeyboard)).MessageId);
            }

            InlineKeyboardMarkup deleteKeyboard = InlineKeyboardMarkup.Empty().WithDeleteButton(messageIds.ToArray());
            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Message.Chat.Id,
                                                       text: "Delete",
                                                       replyMarkup: deleteKeyboard);
        }
    }
}
