using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class DeleteCommand : AbstractCommand<Message, CallbackQuery>
    {
        private ILogger<DeleteCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<DeleteCommand>>();

        public DeleteCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(searchHandler, telegramBotClient, data)
        {
        }

        public override async Task<Message> ExecuteAsync()
        {
            _logger.LogDebug($"Handle [{CommandList.COMMAND_DELETE}] command: [{Data?.Data}]");

            var splitMessage = Data.GetSplitMessageText();

            if (splitMessage.Count() != 1)
            {
                var messageIds = Data.GetParametersFromMessageText(CommandList.COMMAND_DELETE);

                foreach (var messageId in messageIds)
                {
                    await TelegramBotClient.DeleteMessageAsync(Data.Message.Chat.Id, int.Parse(messageId));
                }
            }

            await TelegramBotClient.DeleteMessageAsync(Data.Message.Chat.Id, Data.Message.MessageId);
            return null;
        }
    }
}
