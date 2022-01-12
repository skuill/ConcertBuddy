using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using LyricsScraper;
using LyricsScraper.Abstract;
using LyricsScraper.AZLyrics;
using LyricsScraper.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Abstract;
using SetlistFmAPI;
using SetlistFmAPI.Http;

namespace ConcertBuddy.ConsoleApp
{
    public sealed class ServiceProviderSingleton
    {
        private ServiceProvider _serviceProvider;

        private ServiceProviderSingleton() {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private static readonly Lazy<ServiceProviderSingleton> lazy =
            new Lazy<ServiceProviderSingleton>(() => new ServiceProviderSingleton());

        public static ServiceProviderSingleton Source { get { return lazy.Value; } }

        public T? GetService<T>() => _serviceProvider.GetService<T>();

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                        }))
                    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                    .AddScoped<ISetlistFmClient, SetlistFmClient>()
                    .AddScoped<ISetlistHttpClient, SetlistHttpWebClient>()
                    .AddScoped<ILyricsScraperUtil, LyricsScraperUtil>()
                    .AddScoped<ILyricWebClient, HtmlAgilityWebClient>()
                    .AddScoped<ILyricParser, AZLyricsParser>()
                    .AddScoped<ILyricGetter, AZLyricsGetter>()
                    .AddScoped<IBotHandlers, BotHandlers>()
                    .AddScoped<IMusicSearcherClient, MusicSearcherClient>()
                    .AddScoped<ISearchHandler, SearchHandler>();
        }
    }
}
