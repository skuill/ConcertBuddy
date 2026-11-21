using Hqub.MusicBrainz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;

namespace MusicSearcher.Http;

public static class MusicServiceHttpClients
{
    public const string MusicBrainzClientName = "MusicBrainz";
    public const string SetlistFmClientName = "SetlistFm";

    private const string ProjectUrl = "https://github.com/skuill/ConcertBuddy";

    public static IServiceCollection AddMusicServiceHttpClients(this IServiceCollection services, MusicServiceHttpOptions? options = null)
    {
        options ??= new MusicServiceHttpOptions();

        // RemoveAllLoggers: the default handlers write two lines per request, the retries are logged by the resilience handler.
        var musicBrainz = services.AddHttpClient(MusicBrainzClientName, client =>
            {
                client.BaseAddress = new Uri(MusicBrainzClient.ServiceBaseAddress);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
            })
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
        musicBrainz.AddResilienceHandler(MusicBrainzClientName, builder => ConfigureResilience(builder, options.MusicBrainz, options.MusicBrainzRateLimiter));
        // After the retries, so inside them: every attempt is logged.
        musicBrainz.AddHttpMessageHandler(provider => new MusicBrainzBusyLogger(
            provider.GetRequiredService<ILogger<MusicBrainzBusyLogger>>(),
            provider.GetService<TimeProvider>() ?? TimeProvider.System));

        services.AddHttpClient(SetlistFmClientName)
            .RemoveAllLoggers()
            .AddHttpMessageHandler(() => new EnsureSuccessHandler())
            .AddResilienceHandler(SetlistFmClientName, builder => ConfigureResilience(builder, options.SetlistFm, options.SetlistFmRateLimiter));

        services.AddScoped(provider => new MusicBrainzClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(MusicBrainzClientName)));

        return services;
    }

    private static void ConfigureResilience(ResiliencePipelineBuilder<HttpResponseMessage> builder, ServiceCallPolicy policy, RateLimiter rateLimiter)
    {
        // The order matters. The first timeout is the whole wait of the user: the retries and the time in the queue of the
        // limiter count. Every retry waits for the limiter, and the last timeout is one attempt without the queue.
        builder
            .AddTimeout(policy.TotalTimeout)
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = policy.MaxRetryAttempts,
                Delay = policy.RetryDelay,
                BackoffType = policy.BackoffType,
                // No jitter: one bot instance does not need it to spread its retries.
                UseJitter = false,
                // Our delays, not Retry-After: a busy MusicBrainz sends 503 with "Retry-After: 0" or 1, so the retries came
                // at once and got 503 too. A longer Retry-After would keep the user waiting beyond the attempt timeout anyway.
                ShouldRetryAfterHeader = false,
            })
            .AddRateLimiter(rateLimiter)
            .AddTimeout(policy.AttemptTimeout);
    }

    private static string GetUserAgent()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
        return $"ConcertBuddy/{version} ( {ProjectUrl} )";
    }
}
