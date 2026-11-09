using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

/// <summary>
/// A pseudonymous user key for the logs: HMAC-SHA256 of the Telegram user id with a secret, shortened.
/// The same user always gets the same key, so unique users can be counted, but the id itself is never logged.
/// </summary>
public sealed class UserKeys
{
    public const string NoKey = "-";

    private const int KeyLength = 16;

    private readonly byte[]? _secret;

    public UserKeys(IOptions<Configuration> configuration)
    {
        var secret = configuration.Value.UserKeySecret;
        _secret = string.IsNullOrWhiteSpace(secret) ? null : Encoding.UTF8.GetBytes(secret);
    }

    public string Get(long? userId)
    {
        if (_secret == null || userId == null)
        {
            return NoKey;
        }

        var hash = HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes(userId.Value.ToString(CultureInfo.InvariantCulture)));
        return Convert.ToHexStringLower(hash)[..KeyLength];
    }
}
