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
            string usage = $"Hi, {Data.From?.FirstName}! 👋\n" +
                $"Please, write any artist and I will find him! 🔍";

            return await TelegramBotClient.SendTextMessageAsync(chatId: Data.Chat.Id,
                                                        text: usage,
                                                        replyMarkup: new ReplyKeyboardRemove());
        }
    }
}
