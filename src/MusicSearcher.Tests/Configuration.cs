using Microsoft.Extensions.Logging;
using System;
using System.Configuration;

namespace MusicSearcher.Tests
{
    public class Configuration
    {
        private static readonly ILogger<Configuration>? _logger;
        private static readonly string LASTFM_API_KEY = "LastFmApiKey";
        private static readonly string LASTFM_API_SECRET = "LastFmApiSecret";
        private static readonly string SPOTIFY_CLIENT_ID = "SpotifyClientID";
        private static readonly string SPOTIFY_CLIENT_SECRET = "SpotifyClientSecret";
        private static readonly string YANDEX_TOKEN = "YandexToken";

        public static string LastFmApiKey => ReadSetting(LASTFM_API_KEY);

        public static string LastFmApiSecret => ReadSetting(LASTFM_API_SECRET);

        public static string SpotifyClientID => ReadSetting(SPOTIFY_CLIENT_ID);

        public static string SpotifyClientSecret => ReadSetting(SPOTIFY_CLIENT_SECRET);

        public static string YandexToken => ReadSetting(YANDEX_TOKEN);

        private static string ReadSetting(string key)
        {
            try
            {
                var setting = ConfigurationManager.AppSettings[key];
                if (setting == null)
                {
                    _logger?.LogError($"Config {key} not found");
                    return String.Empty;
                }
                return setting;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error reading app settings {ex}");
            }
            return String.Empty;
        }
    }
}
