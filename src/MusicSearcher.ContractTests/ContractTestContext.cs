using Microsoft.Extensions.Configuration;

namespace MusicSearcher.ContractTests;

/// <summary>
/// Keys of the real services and the shared setup of the contract tests.
/// Keys come from user secrets or from the environment variables CONCERTBUDDY_LastFmApiKey and CONCERTBUDDY_LastFmApiSecret.
/// </summary>
internal static class ContractTestContext
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddUserSecrets(typeof(ContractTestContext).Assembly, optional: true)
        .AddEnvironmentVariables("CONCERTBUDDY_")
        .Build();

    public static string? LastFmApiKey => Configuration["LastFmApiKey"];

    public static string? LastFmApiSecret => Configuration["LastFmApiSecret"];

    /// <summary>
    /// MusicSearcherClient keeps MusicBrainz responses in this folder: without a clean-up a test would read them instead of calling the service.
    /// </summary>
    public static void ClearMusicBrainzCache()
    {
        var cache = Path.Combine(AppContext.BaseDirectory, "cache");
        if (Directory.Exists(cache))
        {
            Directory.Delete(cache, recursive: true);
        }
    }
}
