using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using ConcertBuddy.ConsoleApp.Tests.Host;
using ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicBrainz;
using Shouldly;
using System.Net;

namespace ConcertBuddy.ConsoleApp.Tests.Scenarios;

/// <summary>
/// Commands, the start menu, help and about (#48), and the bot profile set on start.
/// </summary>
public class InfoScenarioTest : BotHostTestBase
{
    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();

    [Theory]
    [InlineData("/start", "/start", "👋 Hi, Tester!")]
    [InlineData("/start@test_bot", "/start", "👋 Hi, Tester!")]
    [InlineData("/help", "/help", "❓ <b>How to use ConcertBuddy</b>")]
    [InlineData("/HELP", "/help", "❓ <b>How to use ConcertBuddy</b>")]
    [InlineData("/about", "/about", "ℹ️ <b>ConcertBuddy</b>")]
    [InlineData("/search", "/search", "🔍 Add a name")]
    [InlineData("/search@test_bot   ", "/search", "🔍 Add a name")]
    [InlineData("/whatever", "unknown", "❓ <b>How to use ConcertBuddy</b>")]
    public async Task Command_NewMessageWithItsScreen(string text, string logName, string expectedStart)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateTextUpdate(1, text));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "sendMessage" });
        telegram.RequestsOf("sendMessage").Single()["text"]!.GetValue<string>().ShouldStartWith(expectedStart);
        HandledLines().Single().ShouldStartWith($"Handled update: Message {logName} in Private chat from user -: Found in ");
        A.CallTo(() => _music.SearchArtistsByName(A<string>._, A<ScoreType>._, A<int>._, A<int>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData("/search Linkin Park")]
    [InlineData("/search@test_bot   Linkin Park ")]
    [InlineData("Linkin Park")]
    public async Task Search_CommandOrText_SearchesTheNameWithoutTheCommand(string text)
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistsByName("Linkin Park", A<ScoreType>._, 5, 0)).Returns(new MusicArtistBase[] { TestArtist.Metallica() });
        var telegram = new FakeTelegram(CreateTextUpdate(1, text));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("sendMessage").Single()["text"]!.GetValue<string>().ShouldBe("🔍 Searching for <b>Linkin Park</b>…");
        HandledLines().Single().ShouldContain("Message /search in Private chat");
    }

    [Theory]
    [InlineData("m", "/start", "👋 Hi, Tester!")]
    [InlineData("w", "/help", "❓ <b>How to use ConcertBuddy</b>")]
    [InlineData("i", "/about", "ℹ️ <b>ConcertBuddy</b>")]
    public async Task Button_InfoScreenInPlace(string data, string logName, string expectedStart)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, data));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "answerCallbackQuery", "editMessageText" });
        var edit = telegram.RequestsOf("editMessageText").Single();
        edit["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        edit["text"]!.GetValue<string>().ShouldStartWith(expectedStart);
        HandledLines().Single().ShouldStartWith($"Handled update: CallbackQuery {logName} in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task Start_ProfileSetBeforeTheUpdates()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateTextUpdate(1, "/start"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert: the menu for private chats and for groups, then the descriptions.
        var profile = telegram.ProfileRequests.ToList();
        profile.Select(x => x.Method).ShouldBe(new[] { "setMyCommands", "setMyCommands", "setMyDescription", "setMyShortDescription" });
        var commands = profile[0].Body["commands"]!.AsArray().Select(x => $"{x!["command"]} - {x["description"]}");
        commands.ShouldBe(new[] { "search - Find an artist: /search Metallica", "help - How to use the bot", "about - Sources, code and support" });
        profile[0].Body["scope"].ShouldBeNull();
        profile[1].Body["scope"]!["type"]!.GetValue<string>().ShouldBe("all_group_chats");
        profile[1].Body["commands"]!.ToJsonString().ShouldBe(profile[0].Body["commands"]!.ToJsonString());
        profile[2].Body["description"]!.GetValue<string>().ShouldBe(BotProfile.Description);
        profile[3].Body["short_description"]!.GetValue<string>().ShouldBe(BotProfile.ShortDescription);
    }

    [Fact]
    public async Task Start_ProfileRejected_OneWarningAndTheBotWorks()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateTextUpdate(1, "/start"))
        {
            ProfileResponse = () => FakeTelegram.Json(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: test failure"}"""),
        };

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        Logs.Entries.Single(x => x.Level == LogLevel.Warning).Message.ShouldBe("Can't set the command menu and descriptions of the bot");
        telegram.ProfileRequests.Count.ShouldBe(1);
        HandledLines().Single().ShouldContain(": Found in ");
    }

    [Fact]
    public void Profile_TextsWithinTelegramLimits()
    {
        BotProfile.Description.Length.ShouldBeLessThanOrEqualTo(512);
        BotProfile.ShortDescription.Length.ShouldBeLessThanOrEqualTo(120);
        BotProfile.Commands.ShouldAllBe(command => command.Description.Length <= 256 && command.Command.Length <= 32);
    }

    private async Task RunUntilHandled(FakeTelegram telegram, int updates)
    {
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == updates);
        await host.StopAsync(Token);
    }
}
