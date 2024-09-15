using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ConcertBuddy.ConsoleApp
{
    public class Program
    {
        private static ILogger<Program>? _logger;

        public static void Main(string[] args)
        {
            _logger = ServiceProviderSingleton.Source.GetService<ILogger<Program>>();

            _logger.LogInformation($"Start ConcertBoddy application.");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionsHandler);

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
            _logger.LogInformation($"Bot {botUser.FirstName} [{botUser.Id}] start listening.");

            Console.ReadLine();

            _logger.LogInformation($"Stop ConcertBoddy application.");
            // Send cancellation request to stop bot
            cts.Cancel();
            return;
        }

        static void UnhandledExceptionsHandler(object sender, UnhandledExceptionEventArgs ex)
        {
            _logger?.LogCritical((ex.ExceptionObject as Exception)?.ToString());
        }
    }
}