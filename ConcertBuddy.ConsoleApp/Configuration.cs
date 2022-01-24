using System;

namespace ConcertBuddy.ConsoleApp
{
    public class Configuration
    {
        public static string TelegramToken { get; set; }

        public static string SetlistFmApiKey { get; set; }

        public static string GeniusClientAccessToken { get; set; }

        public static string LastFmApiKey { get; set; }

        public static string LastFmApiSecret { get; set; }

        public static string SpotifyClientID { get; set; }

        public static string SpotifyClientSecret { get; set; }

        public static string YandexLogin { get; set; }

        public static string YandexPassword { get; set; }

        public static bool IsSetlistFmAvailable() => !string.IsNullOrWhiteSpace(SetlistFmApiKey);

        public static bool IsLastFmAvailable() => !string.IsNullOrWhiteSpace(LastFmApiKey) && !string.IsNullOrWhiteSpace(LastFmApiSecret);
        
        public static bool IsSpotifyAvailable() => !string.IsNullOrWhiteSpace(SpotifyClientID) && !string.IsNullOrWhiteSpace(SpotifyClientSecret);

        public static bool IsYandexAvailable() => !string.IsNullOrWhiteSpace(YandexLogin) && !string.IsNullOrWhiteSpace(YandexPassword);
    }
}
