using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using LyricsScraperNET.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.MusicService.LastFm;
using MusicSearcher.MusicService.SetlistFm;
using MusicSearcher.MusicService.Spotify;
using MusicSearcher.MusicService.Yandex;
using Serilog;
using SetlistNet;

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
            IConfigurationRoot configurationRoot = GetConfiguration();
            services.AddSingleton<IConfigurationRoot>(configurationRoot);

            var appConfiguration = new Configuration();
            configurationRoot.GetSection("Configuration").Bind(appConfiguration);
            services.AddSingleton<Configuration>(appConfiguration);

            services.AddLyricScraperClientService(configurationRoot);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configurationRoot)
                .CreateLogger();

            services.AddLogging(configure => configure.AddSerilog(dispose: true))
                    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                    .AddScoped<SetlistApi>(
                        s => new SetlistApi(Configuration.SetlistFmApiKey!))
                    .AddScoped<SpotifyServiceClient>(
                        s => new SpotifyServiceClient(Configuration.SpotifyClientID!, Configuration.SpotifyClientSecret!))
                    .AddScoped<LastFmServiceClient>(
                        s => new LastFmServiceClient(Configuration.LastFmApiKey!, Configuration.LastFmApiSecret!))
                    .AddScoped<YandexServiceClient>(
                        s => new YandexServiceClient(Configuration.YandexToken!))
                    .AddScoped<ISetlistFmServiceClient>(provider =>
                        {
                            var logger = provider.GetRequiredService<ILogger<SetlistFmServiceClient>>();
                            var token = Configuration.SetlistFmApiKey!;
                            return new SetlistFmServiceClient(logger, token);
                        })
                    .AddScoped<IBotHandlers, BotHandlers>()
                    .AddScoped<IMusicSearcherClient, MusicSearcherClient>();
        }

        private IConfigurationRoot GetConfiguration()
        {
            var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{currentEnvironment}.json", optional: false, reloadOnChange: true)
                .Build();
        }
    }
}
