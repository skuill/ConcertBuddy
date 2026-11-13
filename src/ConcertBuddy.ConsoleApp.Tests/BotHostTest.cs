using ConcertBuddy.ConsoleApp.Tests.Host;
using ConcertBuddy.ConsoleApp.TelegramBot.Handler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Extensions.Logging;
using Shouldly;
using System.Net;

namespace ConcertBuddy.ConsoleApp.Tests;

public class BotHostTest : BotHostTestBase
{
    private static readonly TimeSpan AlmostNothing = TimeSpan.FromMilliseconds(1);

    [Theory]
    [InlineData("--Configuration:TelegramToken=")]
    [InlineData("--Configuration:TelegramToken=   ")]
    public async Task Start_NoTelegramToken_FailsWithClearMessage(string argument)
    {
        // Arrange
        using var host = Program.CreateHost(new[] { argument });

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(Token));

        // Assert
        exception.Message.ShouldContain("Configuration:TelegramToken is required.");
    }

    [Fact]
    public async Task Update_StartCommand_AnswersLogsOneLineAndStopsOnCancellation()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateStartUpdate(1));
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token).WaitAsync(SafetyTimeout, Token);

        // Assert
        telegram.Answers.Single().ShouldContain("Hi, Tester!");
        telegram.PollingStopped.ShouldBeTrue();
        HandledLines().Single().ShouldStartWith("Handled update: Message /start in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task Update_UserKeySecretSet_LogsPseudonymousKeyNotTheUserId()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateStartUpdate(1));
        using var host = CreateHost(telegram, "--Configuration:UserKeySecret=test-secret");
        var expectedKey = host.Services.GetRequiredService<UserKeys>().Get(TesterId);

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        expectedKey.ShouldNotBe(UserKeys.NoKey);
        HandledLines().Single().ShouldStartWith($"Handled update: Message /start in Private chat from user {expectedKey}: Found in ");
        Logs.Entries.ShouldNotContain(x => x.Message.Contains(TesterId.ToString()));
    }

    [Fact]
    public async Task Update_SlowCommand_DoesNotHoldTheNextUpdate()
    {
        // Arrange: the answer to the first update waits until the test opens the gate.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var telegram = new FakeTelegram(CreateStartUpdate(1), CreateStartUpdate(2)) { BeforeAnswer = number => number == 1 ? gate.Task : Task.CompletedTask };
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);

        // Assert: the second update is handled while the first one still waits.
        await WaitUntil(() => HandledLines().Count == 1, () => $"handled while the first update waits: {HandledLines().Count}");
        telegram.Answers.Count.ShouldBe(1);
        gate.SetResult();
        await WaitUntil(() => HandledLines().Count == 2);
        await host.StopAsync(Token);
    }

    [Fact]
    public async Task Stop_UpdateInProgress_WaitsForIt()
    {
        // Arrange
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var telegram = new FakeTelegram(CreateStartUpdate(1)) { BeforeAnswer = _ => gate.Task };
        using var host = CreateHost(telegram);
        await host.StartAsync(Token);
        await WaitUntil(() => telegram.AnswersStarted == 1);

        // Act
        var stop = host.StopAsync(Token);
        await Task.Delay(TimeSpan.FromMilliseconds(100), Token);

        // Assert
        stop.IsCompleted.ShouldBeFalse();
        gate.SetResult();
        await stop.WaitAsync(SafetyTimeout, Token);
        HandledLines().Single().ShouldContain(": Found in ");
    }

    [Fact]
    public async Task Update_CommandFails_LogsItTellsTheUserAndHandlesNextUpdate()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateStartUpdate(1), CreateStartUpdate(2)) { FailedAnswers = 1 };
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 2);
        await host.StopAsync(Token);

        // Assert
        // Updates run in parallel: the order of the answers and lines is not fixed.
        telegram.Answers.Count.ShouldBe(3);
        telegram.Answers.Count(x => x.Contains("Something goes wrong")).ShouldBe(1);
        telegram.Answers.Count(x => x.Contains("Hi, Tester!")).ShouldBe(2);
        Logs.Entries.ShouldContain(x => x.Level == LogLevel.Error && x.Message.StartsWith("Failed to handle update ") && x.Message.EndsWith(" of type Message, command /start"));
        HandledLines().Count(x => x.Contains(": Error in ")).ShouldBe(1);
        HandledLines().Count(x => x.Contains(": Found in ")).ShouldBe(1);
    }

    [Fact]
    public async Task Update_UnknownCallback_OutdatedAndLogLineDoesNotHoldWhatTheUserSent()
    {
        // Arrange
        const string secret = "my-private-text";
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, secret, chatType: "group"));
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery outdated in Group chat from user -: Outdated in ");
        Logs.Entries.ShouldNotContain(x => x.Message.Contains(secret));
    }

    [Fact]
    public async Task Update_OldCommandWithoutParameters_ResultIsErrorAndTheButtonIsAnswered()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, "/biography"));
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery /biography in Private chat from user -: Error in ");
        telegram.Answers.Single().ShouldContain("Please pass any parameters");
        telegram.RequestsOf("answerCallbackQuery").ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Polling_TooManyRequests_WaitsRetryAfter()
    {
        // Arrange: more than RetryThreshold of the client, otherwise the client waits by itself.
        var telegram = new FakeTelegram(CreateStartUpdate(1));
        telegram.PollingFailures.Enqueue(() => FakeTelegram.Json(HttpStatusCode.TooManyRequests, """{"ok":false,"error_code":429,"description":"Too Many Requests","parameters":{"retry_after":120}}"""));
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);

        // Assert
        await AssertNextPollAfter(telegram, TimeSpan.FromSeconds(120), expectedPolls: 1);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);
        Logs.Entries.ShouldContain(x => x.Level == LogLevel.Warning && x.Message.Contains("Next attempt in 120 s"));
    }

    [Fact]
    public async Task Polling_NetworkErrors_GrowingDelaysOneWarningAndOneRecoveryLine()
    {
        // Arrange
        var delays = new[] { 1, 2, 4, 8, 16, 32, 60, 60 };
        var telegram = new FakeTelegram(CreateStartUpdate(1));
        foreach (var _ in delays)
        {
            telegram.PollingFailures.Enqueue(() => throw new HttpRequestException("Connection reset"));
        }
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);
        for (var i = 0; i < delays.Length; i++)
        {
            await AssertNextPollAfter(telegram, TimeSpan.FromSeconds(delays[i]), expectedPolls: i + 1);
        }
        await WaitUntil(() => Logs.Entries.Any(x => x.Message.Contains("works again")), () => State(telegram));
        await host.StopAsync(Token);

        // Assert
        Logs.Entries.Count(x => x.Level >= LogLevel.Warning && x.Message.StartsWith("Telegram polling failed")).ShouldBe(1);
        Logs.Entries.Single(x => x.Message.Contains("works again")).Message.ShouldBe("Telegram polling works again after 8 failed attempts");
    }

    [Fact]
    public async Task Polling_Conflict_IsLoggedAsError()
    {
        // Arrange
        var telegram = new FakeTelegram();
        telegram.PollingFailures.Enqueue(() => FakeTelegram.Json(HttpStatusCode.Conflict, """{"ok":false,"error_code":409,"description":"Conflict: terminated by other getUpdates request"}"""));
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => Logs.Entries.Any(x => x.Message.StartsWith("Telegram polling failed")));
        await host.StopAsync(Token);

        // Assert
        Logs.Entries.Single(x => x.Message.StartsWith("Telegram polling failed")).Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task Polling_InvalidToken_StopsTheApplication()
    {
        // Arrange
        var telegram = new FakeTelegram { GetMeResponse = () => FakeTelegram.Json(HttpStatusCode.Unauthorized, """{"ok":false,"error_code":401,"description":"Unauthorized"}""") };
        using var host = CreateHost(telegram);
        var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => stopping.IsCancellationRequested);

        // Assert
        telegram.PollCalls.ShouldBe(0);
    }

    [Fact]
    public void Logging_ResilienceHandler_OnlyWarningsReachSerilog()
    {
        // Arrange
        using var host = Program.CreateHost(new[] { TokenArgument });

        // Act
        var rules = host.Services.GetRequiredService<IOptions<LoggerFilterOptions>>().Value.Rules;

        // Assert
        rules.ShouldContain(x => x.ProviderName == typeof(SerilogLoggerProvider).FullName && x.CategoryName == "Polly" && x.LogLevel == LogLevel.Warning);
    }

    /// <summary>
    /// The poller waits on the fake clock: the next poll must not come a moment before the delay ends and must come right after it.
    /// </summary>
    private async Task AssertNextPollAfter(FakeTelegram telegram, TimeSpan delay, int expectedPolls)
    {
        await WaitUntil(() => Time.TimersCreated >= expectedPolls, () => $"timer {expectedPolls}: {State(telegram)}");
        telegram.PollCalls.ShouldBe(expectedPolls);

        Time.Advance(delay - AlmostNothing);
        await Task.Delay(TimeSpan.FromMilliseconds(50), Token);
        telegram.PollCalls.ShouldBe(expectedPolls);

        Time.Advance(AlmostNothing);
        await WaitUntil(() => telegram.PollCalls >= expectedPolls + 1, () => $"poll {expectedPolls + 1}: {State(telegram)}");
    }

    private string State(FakeTelegram telegram)
    {
        return $"polls {telegram.PollCalls}, timers {Time.TimersCreated}, logs: {string.Join(" | ", Logs.Entries.Select(x => $"{x.Level} {x.Message}"))}";
    }

    private static string CreateStartUpdate(int updateId)
    {
        return CreateTextUpdate(updateId, "/start");
    }
}
