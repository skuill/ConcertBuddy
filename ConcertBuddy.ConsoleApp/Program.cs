using LyricsScraper;
using LyricsScraper.Abstract;
using LyricsScraper.AZLyrics;
using LyricsScraper.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            string artistName = "Parkway Drive";
            _logger.LogInformation(artistName);
            /// Get setlist for artist

            // sativkv@gmail.com API
            string setlistApiKey = Configuration.SetlistFmApiKey;

            ISetlistFmClient setlistFmClient = serviceProvider.GetService<ISetlistFmClient>();
            setlistFmClient.WithApiKey(setlistApiKey);

            var artists = setlistFmClient.SearchArtists(artistName).GetAwaiter().GetResult();
            var artist = artists.Items.FirstOrDefault();

            var setlists = setlistFmClient.SearchArtistSetlists(artist.MBID).GetAwaiter().GetResult();
            _logger.LogInformation($"setlists: {setlists.Items.Count}");

            /// Get lyric for first song in setlist
            var songName = setlists.Items.FirstOrDefault()
                .Sets.Items.FirstOrDefault()
                .Songs.FirstOrDefault().Name;
            _logger.LogInformation($"Song: {songName}");
            
            ILyricsScraperUtil lyricsScraperUtil = serviceProvider.GetService<ILyricsScraperUtil>();
            ILyricGetter lyricGetter = serviceProvider.GetService<ILyricGetter>();
            lyricsScraperUtil.AddGetter(lyricGetter);
            var lyric = lyricsScraperUtil.SearchLyric(artistName, songName);
            _logger.LogInformation(lyric);
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
                    .AddScoped<IBotHandlers, BotHandlers>();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Type != UpdateType.Message)
                return;
            // Only process text messages
            if (update.Message!.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            _logger.LogInformation($"Received a '{messageText}' message in chat {chatId}.");

            // Echo received message text
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "You said:\n" + messageText,
                cancellationToken: cancellationToken);
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}