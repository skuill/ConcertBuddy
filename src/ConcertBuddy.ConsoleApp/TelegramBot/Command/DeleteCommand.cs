using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class DeleteCommand : AbstractCommand<Message?, CallbackQuery>
    {
        private const string CurrentCommand = CommandList.COMMAND_DELETE;

        private ILogger<DeleteCommand>? _logger = ServiceProviderSingleton.Source.GetService<ILogger<DeleteCommand>>();

        public DeleteCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, CallbackQuery data)
            : base(musicSearcherClient, telegramBotClient, data)
        {
        }

        public override async Task<Message?> ExecuteAsync()
        {
            _logger?.LogDebug($"Handle [{CurrentCommand}] command: [{Data?.Data}]");

            if (Data == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Unexpected case. [Data] field is null.");
                return null;
            }
            if (Data!.Message == null)
            {
                _logger?.LogError($"Command: [{CurrentCommand}]. Unexpected case. [Data.Message] field is null.");
            }

            var splitMessage = Data!.GetSplitMessageText();

            if (splitMessage.Count() != 1)
            {
                var messageIds = Data!.GetParametersFromMessageText(CurrentCommand);

                foreach (var messageId in messageIds)
                {
                    await TelegramBotClient.DeleteMessage(Data!.Message.Chat.Id, int.Parse(messageId));
                }
            }

            await TelegramBotClient.DeleteMessage(Data!.Message.Chat.Id, Data.Message.MessageId);
            
            return null;
        }
    }
}
