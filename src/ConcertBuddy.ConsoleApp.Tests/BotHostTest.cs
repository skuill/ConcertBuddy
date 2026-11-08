using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    [Theory]
    [InlineData("--Configuration:TelegramToken=")]
    [InlineData("--Configuration:TelegramToken=   ")]
    public async Task Start_NoTelegramToken_FailsWithClearMessage(string argument)
    {
        // Arrange
        using var host = Program.CreateHost(new[] { argument });

        // Act
        var exception = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldContain("Configuration:TelegramToken is required.");
    }

    [Fact]
    public async Task Update_StartCommand_AnswersAndStopsOnCancellation()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateStartUpdate(1));
        using var host = CreateHost(telegram, new CapturingLoggerProvider());

        // Act
        await host.StartAsync(TestContext.Current.CancellationToken);
        await telegram.WaitForAnswers(1);
        await host.StopAsync(TestContext.Current.CancellationToken).WaitAsync(SafetyTimeout, TestContext.Current.CancellationToken);

        // Assert
        telegram.Answers.Single().ShouldContain("Hi, Tester!");
        telegram.PollingStopped.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_CommandFails_IsLoggedAndNextUpdateIsHandled()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateStartUpdate(1), CreateStartUpdate(2)) { FailedAnswers = 1 };
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(telegram, logs);

        // Act
        await host.StartAsync(TestContext.Current.CancellationToken);
        await telegram.WaitForAnswers(2);
        await host.StopAsync(TestContext.Current.CancellationToken).WaitAsync(SafetyTimeout, TestContext.Current.CancellationToken);

        // Assert
        telegram.Answers.Count.ShouldBe(2);
        logs.Entries.ShouldContain(x => x.Level == LogLevel.Error && x.Message.Contains("Bad Request: test failure"));
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

    private static IHost CreateHost(FakeTelegram telegram, CapturingLoggerProvider logs)
    {
        return Program.CreateHost(new[] { TokenArgument }, services =>
        {
            services.AddHttpClient(ServiceCollectionExtensions.TelegramClientName).ConfigurePrimaryHttpMessageHandler(() => telegram);
            services.AddSingleton<ILoggerProvider>(logs);
        });
    }

    private static string CreateStartUpdate(int updateId)
    {
        return $$$"""{"update_id":{{{updateId}}},"message":{"message_id":{{{updateId}}},"date":1,"chat":{"id":10,"type":"private"},"from":{"id":5,"is_bot":false,"first_name":"Tester"},"text":"/start"}}""";
    }

    private sealed class FakeTelegram : HttpMessageHandler
    {
        private const string Message = """{"message_id":100,"date":1,"chat":{"id":10,"type":"private"}}""";

        private readonly ConcurrentQueue<string> _updates;
        private readonly SemaphoreSlim _answered = new(0);
        private readonly ConcurrentQueue<string> _answers = new();
        private int _failedAnswers;

        public FakeTelegram(params string[] updates)
        {
            _updates = new ConcurrentQueue<string>(updates);
        }

        public int FailedAnswers { init => _failedAnswers = value; }

        public IReadOnlyCollection<string> Answers => _answers;

        public bool PollingStopped { get; private set; }

        public async Task WaitForAnswers(int count)
        {
            for (var i = 0; i < count; i++)
            {
                (await _answered.WaitAsync(SafetyTimeout)).ShouldBeTrue();
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.RequestUri!.Segments.Last();
            switch (method)
            {
                case "getMe":
                    return Ok("""{"id":123456,"is_bot":true,"first_name":"TestBot","username":"test_bot"}""");
                case "getUpdates":
                    return await GetUpdates(cancellationToken);
                case "sendMessage":
                    return await SendMessage(request, cancellationToken);
                default:
                    throw new InvalidOperationException($"Unexpected Telegram method {method}");
            }
        }

        private async Task<HttpResponseMessage> GetUpdates(CancellationToken cancellationToken)
        {
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
            _answered.Release();
            if (Interlocked.Decrement(ref _failedAnswers) >= 0)
            {
                return Json(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: test failure"}""");
            }
            return Ok(Message);
        }

        private static HttpResponseMessage Ok(string result) => Json(HttpStatusCode.OK, $$"""{"ok":true,"result":{{result}}}""");

        private static HttpResponseMessage Json(HttpStatusCode status, string json)
        {
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
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
