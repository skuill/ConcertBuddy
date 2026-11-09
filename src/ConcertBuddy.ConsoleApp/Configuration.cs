namespace ConcertBuddy.ConsoleApp;

public class Configuration
{
    /// <summary>
    /// Bot token is a key that required to authorize the bot and send requests to the Bot API.
    /// Documentation: https://telegrambots.github.io/book/1/quickstart.html
    /// </summary>
    public string? TelegramToken { get; set; }

    /// <summary>
    /// Setlist.fm API key from https://www.setlist.fm/settings/api (link for logged in users only) 
    /// If you're no registered user yet, then register first (it's free): https://www.setlist.fm/signup
    /// Documentation: https://api.setlist.fm/docs/1.0/index.html
    /// </summary>
    public string? SetlistFmApiKey { get; set; }

    /// <summary>
    /// API key and secret need to authenticating with the API.
    /// Documentation: https://www.last.fm/api/authentication
    /// </summary>
    public string? LastFmApiKey { get; set; }

    /// <summary>
    /// API key and secret need to authenticating with the API.
    /// Documentation: https://www.last.fm/api/authentication
    /// </summary>
    public string? LastFmApiSecret { get; set; }

    /// <summary>
    /// To access Yandex API you need authorization. You can do it via OAuth token
    /// Documentation 1: https://yandexmusicapicsharp.readthedocs.io/ru/latest/client/root.html
    /// Documentation 2: https://yandex-music.readthedocs.io/en/main/
    /// </summary>
    public string? YandexToken { get; set; }

    /// <summary>
    /// Secret of the pseudonymous user key in the logs (HMAC of the Telegram user id). Without it the logs have no user key.
    /// Any long random string, for example from `openssl rand -hex 32`. Changing it starts the counting of users anew.
    /// </summary>
    public string? UserKeySecret { get; set; }

    public bool IsSetlistFmAvailable() => !string.IsNullOrWhiteSpace(SetlistFmApiKey);

    public bool IsLastFmAvailable() => !string.IsNullOrWhiteSpace(LastFmApiKey) && !string.IsNullOrWhiteSpace(LastFmApiSecret);

    public bool IsYandexAvailable() => !string.IsNullOrEmpty(YandexToken);
}
