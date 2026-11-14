using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using LyricsScraperNET.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicSearcher;
using MusicSearcher.Http;
using MusicSearcher.MusicService.Abstract;
using MusicSearcher.MusicService.LastFm;
using MusicSearcher.MusicService.SetlistFm;
using MusicSearcher.MusicService.Yandex;
using Serilog;
using Serilog.Extensions.Logging;
using SetlistNet;
using Telegram.Bot;

namespace ConcertBuddy.ConsoleApp;

public static class ServiceCollectionExtensions
{
    public const string TelegramClientName = "Telegram";

    /// <summary>
    /// Downloads the audio of a track, so the bot uploads it with a readable file name.
    /// </summary>
    public const string AudioClientName = "Audio";

    private const string ConfigurationSectionName = "Configuration";

    public static IServiceCollection AddConcertBuddy(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationSectionName);
        var settings = section.Get<Configuration>() ?? new Configuration();

        services.AddOptions<Configuration>()
            .Bind(section)
            .Validate(x => !string.IsNullOrWhiteSpace(x.TelegramToken), $"{ConfigurationSectionName}:{nameof(Configuration.TelegramToken)} is required.")
            .ValidateOnStart();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        // AddSerilog adds a rule for its provider, and only a rule for the same provider can override it.
        services.AddLogging(configure => configure.AddSerilog(dispose: true).AddFilter<SerilogLoggerProvider>("Polly", LogLevel.Warning));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<UserKeys>();

        // Screens (#48): the codec keeps the long button data of all chats, so it lives as long as the bot.
        services.AddSingleton<CallbackCodec>();
        services.AddTransient<ScreenSender>();
        services.AddScoped<SearchScreens>();
        services.AddScoped<ArtistScreens>();
        services.AddScoped<SetlistScreens>();
        services.AddScoped<TrackScreens>();
        services.AddHttpClient(AudioClientName, client => client.Timeout = TimeSpan.FromSeconds(60)).RemoveAllLoggers();

        services.AddHttpClient(TelegramClientName)
                .RemoveAllLoggers()
                .AddTypedClient<ITelegramBotClient>((httpClient, provider) => new TelegramBotClient(
                    provider.GetRequiredService<IOptions<Configuration>>().Value.TelegramToken!,
                    httpClient));

        services.AddLyricScraperClientService(configuration);
        services.AddMusicServiceHttpClients()
                .AddScoped<SetlistApi>(
                    s => new SetlistApi(
                        settings.SetlistFmApiKey!,
                        httpClient: s.GetRequiredService<IHttpClientFactory>().CreateClient(MusicServiceHttpClients.SetlistFmClientName)))
                .AddScoped<ISetlistFmServiceClient, SetlistFmServiceClient>()
                .AddScoped<IBotHandlers, BotHandlers>()
                .AddScoped<IMusicSearcherClient, MusicSearcherClient>();

        if (settings.IsYandexAvailable())
        {
            services.AddScoped<IMusicServiceClient>(s => new YandexServiceClient(settings.YandexToken!));
        }

        if (settings.IsLastFmAvailable())
        {
            services.AddScoped<IMusicServiceClient>(s => new LastFmServiceClient(settings.LastFmApiKey!, settings.LastFmApiSecret!));
        }

        services.AddHostedService<BotPollingService>();

        return services;
    }
}
