using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using LyricsScraperNET.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Http;
using MusicSearcher.MusicService.Abstract;
using MusicSearcher.MusicService.LastFm;
using MusicSearcher.MusicService.SetlistFm;
using MusicSearcher.MusicService.Yandex;
using Serilog;
using SetlistNet;

namespace ConcertBuddy.ConsoleApp;

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

        services.AddLogging(configure => configure.AddSerilog(dispose: true).AddFilter("Polly", LogLevel.Warning))
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                .AddMusicServiceHttpClients()
                .AddScoped<SetlistApi>(
                    s => new SetlistApi(
                        Configuration.SetlistFmApiKey!,
                        httpClient: s.GetRequiredService<IHttpClientFactory>().CreateClient(MusicServiceHttpClients.SetlistFmClientName)))
                .AddScoped<IMusicServiceClient>(
                    s => new YandexServiceClient(Configuration.YandexToken!))
                .AddScoped<IMusicServiceClient>(
                    s => new LastFmServiceClient(Configuration.LastFmApiKey!, Configuration.LastFmApiSecret!))
                .AddScoped<ISetlistFmServiceClient, SetlistFmServiceClient>()
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
