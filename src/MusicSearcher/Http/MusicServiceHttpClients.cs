using Hqub.MusicBrainz;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
        services.AddHttpClient(MusicBrainzClientName, client =>
            {
                client.BaseAddress = new Uri(MusicBrainzClient.ServiceBaseAddress);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
            })
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
            .AddResilienceHandler(MusicBrainzClientName, builder => ConfigureResilience(builder, options, options.MusicBrainzRateLimiter));

        services.AddHttpClient(SetlistFmClientName)
            .RemoveAllLoggers()
            .AddHttpMessageHandler(() => new EnsureSuccessHandler())
            .AddResilienceHandler(SetlistFmClientName, builder => ConfigureResilience(builder, options, options.SetlistFmRateLimiter));

        services.AddScoped(provider => new MusicBrainzClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(MusicBrainzClientName)));

        return services;
    }

    private static void ConfigureResilience(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        MusicServiceHttpOptions options,
        RateLimiter rateLimiter)
    {
        // The order matters: every retry waits for the limiter, and the timeout does not count the time in its queue.
        builder
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = options.RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            })
            .AddRateLimiter(rateLimiter)
            .AddTimeout(options.AttemptTimeout);
    }

    private static string GetUserAgent()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
        return $"ConcertBuddy/{version} ( {ProjectUrl} )";
    }
}
