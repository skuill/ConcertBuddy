using ConcertBuddy.ConsoleApp.Search;
using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using LyricsScraperNET.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Abstract;
using Serilog;
using SetlistFmAPI;
using SetlistFmAPI.Http;

namespace ConcertBuddy.ConsoleApp
{
    public sealed class ServiceProviderSingleton
    {
        private ServiceProvider _serviceProvider;

        private ServiceProviderSingleton()
        {
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
            services.AddLogging(configure => configure.AddSerilog(dispose: true))
                    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                    .AddScoped<ISetlistFmClient, SetlistFmClient>()
                    .AddScoped<ISetlistHttpClient, SetlistHttpWebClient>()
                    .AddScoped<IBotHandlers, BotHandlers>()
                    .AddScoped<IMusicSearcherClient, MusicSearcherClient>()
                    .AddScoped<ISearchHandler, SearchHandler>();

            IConfigurationRoot configurationRoot = GetConfiguration();
            services.AddSingleton<IConfigurationRoot>(configurationRoot);

            services.AddLyricScraperClientService(configurationRoot);

            var appConfiguration = new Configuration();
            configurationRoot.GetSection("Configuration").Bind(appConfiguration);
            services.AddSingleton<Configuration>(appConfiguration);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configurationRoot)
                .CreateLogger();
        }

        private IConfigurationRoot GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }
    }
}
