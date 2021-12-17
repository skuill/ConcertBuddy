using LyricsScraper;
using LyricsScraper.Abstract;
using LyricsScraper.AZLyrics;
using LyricsScraper.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher.Abstract;
using MusicSearcher.MusicBrainz;
using SetlistFmAPI;
using SetlistFmAPI.Http;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ConcertBuddy.ConsoleApp
{
    public class Program
    {
        // Spotify
        // Client ID c75856d9e7454368aca6fe620a103d29

        private static ILogger<Program> _logger = null;

        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            _logger = serviceProvider.GetService<ILogger<Program>>();

            var botClient = new TelegramBotClient(Configuration.TelegramToken);

            IBotHandlers botHandler = serviceProvider.GetService<IBotHandlers>();

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

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole())
                    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                    .AddScoped<ISetlistFmClient, SetlistFmClient>()
                    .AddScoped<ISetlistHttpClient, SetlistHttpWebClient>()
                    .AddScoped<ILyricsScraperUtil, LyricsScraperUtil>()
                    .AddScoped<ILyricWebClient, HtmlAgilityWebClient>()
                    .AddScoped<ILyricParser, AZLyricsParser>()
                    .AddScoped<ILyricGetter, AZLyricsGetter>()
                    .AddScoped<IBotHandlers, BotHandlers>()
                    .AddScoped<IMusicSearcherClient, MusicBrainzSearcherClient>()
                    .AddScoped<ISearchHandler, SearchHandler>();
        }
    }
}