using System;

namespace ConcertBuddy.ConsoleApp
{
    public class Configuration
    {
        /// <summary>
        /// Bot token is a key that required to authorize the bot and send requests to the Bot API.
        /// Documentation: https://telegrambots.github.io/book/1/quickstart.html
        /// </summary>
        public static string TelegramToken { get; set; }

        /// <summary>
        /// Setlist.fm API key from https://www.setlist.fm/settings/api (link for logged in users only) 
        /// If you're no registered user yet, then register first (it's free): https://www.setlist.fm/signup
        /// Documentation: https://api.setlist.fm/docs/1.0/index.html
        /// </summary>
        public static string SetlistFmApiKey { get; set; }

        /// <summary>
        /// API key and secret need to authenticating with the API.
        /// Documentation: https://www.last.fm/api/authentication
        /// </summary>
        public static string LastFmApiKey { get; set; }

        /// <summary>
        /// API key and secret need to authenticating with the API.
        /// Documentation: https://www.last.fm/api/authentication
        /// </summary>
        public static string LastFmApiSecret { get; set; }

        /// <summary>
        /// The Client Credentials flow is used in server-to-server authentication. 
        /// Only endpoints that do not access user information can be accessed. 
        /// By supplying your SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET, you get an access token.
        /// Documentation:
        /// 1) Official: https://developer.spotify.com/documentation/web-api/quick-start/
        /// 2) NET Libraty: https://johnnycrazy.github.io/SpotifyAPI-NET/docs/client_credentials
        /// </summary>
        public static string SpotifyClientID { get; set; }

        /// <summary>
        /// The Client Credentials flow is used in server-to-server authentication. 
        /// Only endpoints that do not access user information can be accessed. 
        /// By supplying your SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET, you get an access token.
        /// Documentation:
        /// 1) Official: https://developer.spotify.com/documentation/web-api/quick-start/
        /// 2) NET Libraty: https://johnnycrazy.github.io/SpotifyAPI-NET/docs/client_credentials
        /// </summary>
        public static string SpotifyClientSecret { get; set; }

        /// <summary>
        /// To access Yandex API you need authorization. You can do it via OAuth token
        /// Documentation 1: https://yandexmusicapicsharp.readthedocs.io/ru/latest/client/root.html
        /// Documentation 2: https://yandex-music.readthedocs.io/en/main/
        /// </summary>
        public static string YandexToken { get; set; }

        public static bool IsSetlistFmAvailable() => !string.IsNullOrWhiteSpace(SetlistFmApiKey);

        public static bool IsLastFmAvailable() => !string.IsNullOrWhiteSpace(LastFmApiKey) && !string.IsNullOrWhiteSpace(LastFmApiSecret);
        
        public static bool IsSpotifyAvailable() => !string.IsNullOrWhiteSpace(SpotifyClientID) && !string.IsNullOrWhiteSpace(SpotifyClientSecret);

        public static bool IsYandexAvailable() => !string.IsNullOrEmpty(YandexToken);
    }
}
