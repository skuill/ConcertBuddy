using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace ConcertBuddy.ConsoleApp
{
    public class Program
    {
        // Spotify
        // Client ID c75856d9e7454368aca6fe620a103d29

        private static ILogger<Program> _logger = null;

        public static void Main(string[] args)
        {
            _logger = ServiceProviderSingleton.Source.GetService<ILogger<Program>>();

            var botClient = new TelegramBotClient(Configuration.TelegramToken);

            IBotHandlers botHandler = ServiceProviderSingleton.Source.GetService<IBotHandlers>();

            using var cts = new CancellationTokenSource();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };
            botClient.StartReceiving(
                botHandler.HandleUpdateAsync,
                botHandler.HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            var botUser = botClient.GetMeAsync().GetAwaiter().GetResult();
            
            Console.Title = botUser.Username ?? "My awesome Bot";
            _logger.LogInformation($"Start listening for user {botUser.Id} with name {botUser.FirstName}.");

            Console.ReadLine();

            // Send cancellation request to stop bot
            cts.Cancel();
            return;
        }
    }
}