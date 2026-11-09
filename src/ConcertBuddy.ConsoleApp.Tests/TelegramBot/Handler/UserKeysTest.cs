using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Handler;

public class UserKeysTest
{
    private const long UserId = 987654321;

    [Fact]
    public void Get_SameUser_SameKey()
    {
        // Arrange
        var keys = Create("secret");

        // Act
        var first = keys.Get(UserId);
        var second = keys.Get(UserId);

        // Assert
        first.ShouldBe(second);
        first.Length.ShouldBe(16);
        first.ShouldMatch("^[0-9a-f]{16}$");
        first.ShouldNotContain(UserId.ToString());
    }

    [Fact]
    public void Get_KnownSecretAndId_IsHmacSha256()
    {
        // Act: the first 16 hex digits of HMAC-SHA256(key "secret", message "987654321").
        var key = Create("secret").Get(UserId);

        // Assert
        key.ShouldBe(Convert.ToHexStringLower(System.Security.Cryptography.HMACSHA256.HashData("secret"u8, "987654321"u8))[..16]);
    }

    [Fact]
    public void Get_OtherUserOrOtherSecret_OtherKey()
    {
        // Act
        var key = Create("secret").Get(UserId);

        // Assert
        Create("secret").Get(UserId + 1).ShouldNotBe(key);
        Create("another secret").Get(UserId).ShouldNotBe(key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Get_NoSecret_NoKey(string? secret)
    {
        // Act
        var key = Create(secret).Get(UserId);

        // Assert
        key.ShouldBe(UserKeys.NoKey);
    }

    [Fact]
    public void Get_NoUser_NoKey()
    {
        // Act
        var key = Create("secret").Get(null);

        // Assert
        key.ShouldBe(UserKeys.NoKey);
    }

    private static UserKeys Create(string? secret)
    {
        return new UserKeys(Options.Create(new Configuration { UserKeySecret = secret }));
    }
}
