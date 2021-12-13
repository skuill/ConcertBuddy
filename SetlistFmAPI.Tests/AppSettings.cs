﻿using Microsoft.Extensions.Logging;
using System;
using System.Configuration;

namespace SetlistFmAPI.Tests
{
    public class AppSettings
    {
        private static readonly ILogger<AppSettings> _logger;
        private static readonly string SETLISTFM_API_KEY = "SetlistFmApiKey";

        public static string SetlistFmApiKey => ReadSetting(SETLISTFM_API_KEY);

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
