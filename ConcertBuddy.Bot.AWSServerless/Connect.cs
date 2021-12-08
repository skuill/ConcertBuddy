using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConcertBuddy.Bot.AWSServerless
{
    public class Connect
    {
        private readonly TelegramBotClient botClient;
        private readonly ILogger<Connect> logger;

        public Connect()
        {
            this.botClient = new TelegramBotClient(AppSettings.TelegramToken);
        }

        public async Task RespFromTelegram(Update update) {
            if (update == null)
            {
                return;
            }

            if (update.Type != UpdateType.Message)
            {
                return;
            }

            var message = update.Message;

            this.logger?.LogInformation("Received Message from {0}", message.Chat.Id);

            switch (message.Type)
            {
                case MessageType.Text:
                    // Echo each Message
                    await this.botClient.SendTextMessageAsync(message.Chat.Id, message.Text);
                    break;
            }
        }
    }
}
