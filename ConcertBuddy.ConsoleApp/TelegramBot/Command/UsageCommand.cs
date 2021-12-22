using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command
{
    public class UsageCommand : AbstractCommand<Message, Message>
    {
        private ILogger<UsageCommand> _logger = ServiceProviderSingleton.Source.GetService<ILogger<UsageCommand>>();

        public UsageCommand(ISearchHandler searchHandler, ITelegramBotClient telegramBotClient, Message data) : base(searchHandler, telegramBotClient, data)
        {
        }

        public async Task<Message> Execute()
        {
            const string usage = "Usage:\n" +
                                 $"{CommandList.COMMAND_SEARCH}   - search artist's biography, setlists and lyrics for songs\n";

            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Chat.Id,
                                                        text: usage,
                                                        replyMarkup: new ReplyKeyboardRemove());
        }
    }
}
