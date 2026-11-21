using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

public class ScreenVersionsTest
{
    private const long ChatId = 10;
    private const int MessageId = 100;

    private readonly ScreenVersions _versions = new();

    [Fact]
    public void Begin_NewerTapOnTheMessage_OnlyTheNewerIsLatest()
    {
        // Arrange
        var older = _versions.Begin(ChatId, MessageId);

        // Act
        var newer = _versions.Begin(ChatId, MessageId);

        // Assert
        _versions.IsLatest(ChatId, MessageId, older).ShouldBeFalse();
        _versions.IsLatest(ChatId, MessageId, newer).ShouldBeTrue();
    }

    [Theory]
    [InlineData(ChatId, MessageId + 1)]
    [InlineData(ChatId + 1, MessageId)]
    public void Begin_TapOnAnotherMessage_BothAreLatest(long otherChatId, int otherMessageId)
    {
        // Arrange
        var first = _versions.Begin(ChatId, MessageId);

        // Act
        var other = _versions.Begin(otherChatId, otherMessageId);

        // Assert
        _versions.IsLatest(ChatId, MessageId, first).ShouldBeTrue();
        _versions.IsLatest(otherChatId, otherMessageId, other).ShouldBeTrue();
    }

    [Fact]
    public void End_OlderTapEndsFirst_NewerStaysLatestAndTheMessageIsKept()
    {
        // Arrange
        var older = _versions.Begin(ChatId, MessageId);
        var newer = _versions.Begin(ChatId, MessageId);

        // Act
        _versions.End(ChatId, MessageId, older);

        // Assert
        _versions.IsLatest(ChatId, MessageId, newer).ShouldBeTrue();
        _versions.Count.ShouldBe(1);
    }

    [Fact]
    public void End_NewerTapEndsFirst_OlderIsNotLatestAndNothingIsKept()
    {
        // Arrange
        var older = _versions.Begin(ChatId, MessageId);
        var newer = _versions.Begin(ChatId, MessageId);

        // Act
        _versions.End(ChatId, MessageId, newer);
        var olderIsLatest = _versions.IsLatest(ChatId, MessageId, older);
        _versions.End(ChatId, MessageId, older);

        // Assert
        olderIsLatest.ShouldBeFalse();
        _versions.Count.ShouldBe(0);
    }
}
