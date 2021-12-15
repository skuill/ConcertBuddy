using Microsoft.Extensions.Logging;
using System.Configuration;

namespace ConcertBuddy.ConsoleApp
{
    public class Configuration
    {
        private static readonly ILogger<Configuration> _logger;
        private static readonly string SETLISTFM_API_KEY = "SetlistFmApiKey";
        private static readonly string GENIUS_CLIENT_ACCESS_TOKEN_KEY = "GeniusClientAccessToken";
        private static readonly string TELEGRAM_TOKEN_KEY = "TelegramToken";

        public static string TelegramToken => ReadSetting(TELEGRAM_TOKEN_KEY);

        public static string SetlistFmApiKey => ReadSetting(SETLISTFM_API_KEY);

        public static string GeniusClientAccessToken => ReadSetting(GENIUS_CLIENT_ACCESS_TOKEN_KEY);

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
