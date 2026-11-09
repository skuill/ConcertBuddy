using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Serilog.Extensions.Logging;
using Shouldly;
using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace ConcertBuddy.ConsoleApp.Tests;

public class BotHostTest
{
    private const string TokenArgument = "--Configuration:TelegramToken=123456:test-token";
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AlmostNothing = TimeSpan.FromMilliseconds(1);

    private readonly CapturingLoggerProvider _logs = new();
    private readonly CountingTimeProvider _time = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

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
        HandledLines().Single().ShouldStartWith("Handled update: Message /start in Private chat: Found in ");
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
        telegram.Answers.Count.ShouldBe(3);
        telegram.Answers.ElementAt(1).ShouldContain("Something goes wrong");
        telegram.Answers.ElementAt(2).ShouldContain("Hi, Tester!");
        _logs.Entries.ShouldContain(x => x.Level == LogLevel.Error && x.Message.StartsWith("Failed to handle update 1 of type Message, command /start"));
        HandledLines()[0].ShouldContain(": Error in ");
        HandledLines()[1].ShouldContain(": Found in ");
    }

    [Fact]
    public async Task Update_UnknownCallback_LogLineDoesNotHoldWhatTheUserSent()
    {
        // Arrange
        const string secret = "my-private-text";
        var update = $$$$"""{"update_id":1,"callback_query":{"id":"7","from":{"id":5,"is_bot":false,"first_name":"Tester"},"data":"{{{{secret}}}}","chat_instance":"1","message":{"message_id":1,"date":1,"chat":{"id":10,"type":"group","title":"T"},"text":"x"}}}""";
        using var host = CreateHost(new FakeTelegram(update));

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery unknown in Group chat: Found in ");
        _logs.Entries.ShouldNotContain(x => x.Message.Contains(secret));
    }

    [Fact]
    public async Task Update_CallbackWithoutParameters_ResultIsError()
    {
        // Arrange
        var update = """{"update_id":1,"callback_query":{"id":"7","from":{"id":5,"is_bot":false,"first_name":"Tester"},"data":"/artist","chat_instance":"1","message":{"message_id":1,"date":1,"chat":{"id":10,"type":"private"},"text":"x"}}}""";
        var telegram = new FakeTelegram(update);
        using var host = CreateHost(telegram);

        // Act
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery /artist in Private chat: Error in ");
        telegram.Answers.Single().ShouldContain("Please pass any parameters");
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
        _logs.Entries.ShouldContain(x => x.Level == LogLevel.Warning && x.Message.Contains("Next attempt in 120 s"));
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
        await WaitUntil(() => _logs.Entries.Any(x => x.Message.Contains("works again")), () => State(telegram));
        await host.StopAsync(Token);

        // Assert
        _logs.Entries.Count(x => x.Level >= LogLevel.Warning && x.Message.StartsWith("Telegram polling failed")).ShouldBe(1);
        _logs.Entries.Single(x => x.Message.Contains("works again")).Message.ShouldBe("Telegram polling works again after 8 failed attempts");
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
        await WaitUntil(() => _logs.Entries.Any(x => x.Message.StartsWith("Telegram polling failed")));
        await host.StopAsync(Token);

        // Assert
        _logs.Entries.Single(x => x.Message.StartsWith("Telegram polling failed")).Level.ShouldBe(LogLevel.Error);
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

    private IHost CreateHost(FakeTelegram telegram)
    {
        return Program.CreateHost(new[] { TokenArgument }, services =>
        {
            services.AddHttpClient(ServiceCollectionExtensions.TelegramClientName).ConfigurePrimaryHttpMessageHandler(() => telegram);
            services.AddSingleton<ILoggerProvider>(_logs);
            services.AddSingleton<TimeProvider>(_time);
        });
    }

    private List<string> HandledLines()
    {
        return _logs.Entries.Where(x => x.Message.StartsWith("Handled update:")).Select(x => x.Message).ToList();
    }

    /// <summary>
    /// The poller waits on the fake clock: the next poll must not come a moment before the delay ends and must come right after it.
    /// </summary>
    private async Task AssertNextPollAfter(FakeTelegram telegram, TimeSpan delay, int expectedPolls)
    {
        await WaitUntil(() => _time.TimersCreated >= expectedPolls, () => $"timer {expectedPolls}: {State(telegram)}");
        telegram.PollCalls.ShouldBe(expectedPolls);

        _time.Advance(delay - AlmostNothing);
        await Task.Delay(TimeSpan.FromMilliseconds(50), Token);
        telegram.PollCalls.ShouldBe(expectedPolls);

        _time.Advance(AlmostNothing);
        await WaitUntil(() => telegram.PollCalls >= expectedPolls + 1, () => $"poll {expectedPolls + 1}: {State(telegram)}");
    }

    private string State(FakeTelegram telegram)
    {
        return $"polls {telegram.PollCalls}, timers {_time.TimersCreated}, logs: {string.Join(" | ", _logs.Entries.Select(x => $"{x.Level} {x.Message}"))}";
    }

    private static async Task WaitUntil(Func<bool> condition, Func<string>? describe = null)
    {
        var deadline = DateTime.UtcNow + SafetyTimeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(describe?.Invoke() ?? "The condition was not met in time.");
            }
            await Task.Delay(5, Token);
        }
    }

    private static string CreateStartUpdate(int updateId)
    {
        return $$$"""{"update_id":{{{updateId}}},"message":{"message_id":{{{updateId}}},"date":1,"chat":{"id":10,"type":"private"},"from":{"id":5,"is_bot":false,"first_name":"Tester"},"text":"/start"}}""";
    }

    private sealed class CountingTimeProvider : FakeTimeProvider
    {
        private int _timersCreated;

        public int TimersCreated => Volatile.Read(ref _timersCreated);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            Interlocked.Increment(ref _timersCreated);
            return timer;
        }
    }

    private sealed class FakeTelegram : HttpMessageHandler
    {
        private const string Message = """{"message_id":100,"date":1,"chat":{"id":10,"type":"private"}}""";

        private readonly ConcurrentQueue<string> _updates;
        private readonly ConcurrentQueue<string> _answers = new();
        private int _failedAnswers;
        private int _pollCalls;

        public FakeTelegram(params string[] updates)
        {
            _updates = new ConcurrentQueue<string>(updates);
        }

        public int FailedAnswers { init => _failedAnswers = value; }

        public Func<HttpResponseMessage> GetMeResponse { get; init; } = () => Ok("""{"id":123456,"is_bot":true,"first_name":"TestBot","username":"test_bot"}""");

        public ConcurrentQueue<Func<HttpResponseMessage>> PollingFailures { get; } = new();

        public IReadOnlyCollection<string> Answers => _answers;

        public int PollCalls => Volatile.Read(ref _pollCalls);

        public bool PollingStopped { get; private set; }

        public static HttpResponseMessage Json(HttpStatusCode status, string json)
        {
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.RequestUri!.Segments.Last();
            switch (method)
            {
                case "getMe":
                    return GetMeResponse();
                case "getUpdates":
                    return await GetUpdates(cancellationToken);
                case "sendMessage":
                    return await SendMessage(request, cancellationToken);
                case "answerCallbackQuery":
                    return Ok("true");
                default:
                    throw new InvalidOperationException($"Unexpected Telegram method {method}");
            }
        }

        private async Task<HttpResponseMessage> GetUpdates(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _pollCalls);
            if (PollingFailures.TryDequeue(out var failure))
            {
                return failure();
            }

            if (_updates.TryDequeue(out var update))
            {
                return Ok($"[{update}]");
            }

            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                PollingStopped = true;
                throw;
            }
            return Ok("[]");
        }

        private async Task<HttpResponseMessage> SendMessage(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _answers.Enqueue(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (Interlocked.Decrement(ref _failedAnswers) >= 0)
            {
                return Json(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: test failure"}""");
            }
            return Ok(Message);
        }

        private static HttpResponseMessage Ok(string result) => Json(HttpStatusCode.OK, $$"""{"ok":true,"result":{{result}}}""");
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                entries.Enqueue((logLevel, formatter(state, exception)));
            }
        }
    }
}
