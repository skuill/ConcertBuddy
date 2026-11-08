using ConcertBuddy.ConsoleApp.TelegramBot.Helper;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Helper;

public class StringHelperTest
{
    [Fact]
    public void SubstringByByteLength_RussianTextLongerThanLimit_CutByBytes()
    {
        // Arrange
        string testData = "/track 827e1b52-1782-4e73-b8e7-a7b6939370f5 Стволок за поясок";

        // Act
        string result = StringHelper.SubstringByByteLength(testData, 64);

        // Assert
        result.ShouldBe("/track 827e1b52-1782-4e73-b8e7-a7b6939370f5 Стволок за ");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("exact")]
    public void SubstringByByteLength_NotLongerThanLimit_ReturnsSameText(string? testData)
    {
        // Act
        string result = StringHelper.SubstringByByteLength(testData!, 5);

        // Assert
        result.ShouldBe(testData);
    }

    [Fact]
    public void SubstringByByteLength_TwoByteLetterOnTheLimit_NotCutInHalf()
    {
        // Act
        string result = StringHelper.SubstringByByteLength("ЯЯЯ", 5);

        // Assert
        result.ShouldBe("ЯЯ");
    }

    [Fact]
    public void SubstringByByteLength_EmojiOnTheLimit_NotCutInHalf()
    {
        // Act
        string result = StringHelper.SubstringByByteLength("🎸🎸", 6);

        // Assert
        result.ShouldBe("🎸");
    }
}
