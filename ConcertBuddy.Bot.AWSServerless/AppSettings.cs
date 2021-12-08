using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace ConcertBuddy.Bot.AWSServerless
{
    public class AppSettings
    {
        private static readonly ILogger<AppSettings> _logger;
        private static readonly string TELEGRAM_TOKEN_KEY = "TelegramToken";

        public static string TelegramToken => ReadSetting(TELEGRAM_TOKEN_KEY);

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
